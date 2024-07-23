using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Dialog;
using DpMapSubscribeTool.Services.Notifications;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.FYS;
using DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.Test;
using DpMapSubscribeTool.Utils;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.ViewModels.Pages.Setting;

public partial class SettingPageViewModel : PageViewModelBase
{
    private readonly IApplicationNotification applicationNotification;
    private readonly IDialogManager dialogManager;

    private readonly IFysServerServiceBase fysServerService;
    private readonly ILogger<SettingPageViewModel> logger;
    private readonly IPersistence persistence;

    [ObservableProperty]
    private ApplicationSettings applicationSettings;

    [ObservableProperty]
    private CustomServerSettings customServerSettings;

    [ObservableProperty]
    private FysServerSettings fysServerSettings;

    [ObservableProperty]
    private string fysTestUserNameResult;

    [ObservableProperty]
    private ITestServerManager testServerManager;

    public SettingPageViewModel()
    {
        DesignModeHelper.CheckOnlyForDesignMode();
    }

    public SettingPageViewModel(ILogger<SettingPageViewModel> logger,
        IPersistence persistence, IDialogManager dialogManager, ITestServerManager testServerManager,
        IApplicationNotification applicationNotification,
        IFysServerServiceBase fysServerService)
    {
        this.logger = logger;
        this.persistence = persistence;
        this.dialogManager = dialogManager;
        this.testServerManager = testServerManager;
        this.applicationNotification = applicationNotification;
        this.fysServerService = fysServerService;

        Initialize();
    }

    public MapSubscribeRule[] MapSubscribeRuleEnums { get; } = Enum.GetValues<MapSubscribeRule>();

    public override string Title => "Setting";

    private async void Initialize()
    {
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif

        ApplicationSettings = await persistence.Load<ApplicationSettings>();
        FysServerSettings = await persistence.Load<FysServerSettings>();
        CustomServerSettings = await persistence.Load<CustomServerSettings>();
    }

    [RelayCommand]
    private void CreateNewRule()
    {
        var newSubscribeRule = new MapSubscribe
        {
            Enable = true,
            MatchContent = "ze_ff",
            MatchRule = MapSubscribeRule.CustomRegex,
            Name = "我要当FFC"
        };

        ApplicationSettings.UserMapSubscribes.Add(newSubscribeRule);
        logger.LogInformationEx("User add new subscribe rule.");
    }

    [RelayCommand]
    private void CreateNewCustomServerInfo()
    {
        var serverInfo = new ServerInfo
        {
            Host = "localhost",
            Port = 2857,
            ServerGroupDisplay = "反身盖土叛忍社",
            ServerGroup = "Custom",
            Name = "某个跳刀蛆の服务器"
        };

        CustomServerSettings.CustomServerInfos.Add(serverInfo);
        logger.LogInformationEx("User add new custom server info.");
    }

    [RelayCommand]
    private void DeleteCustomServerInfo(ServerInfo serverInfo)
    {
        CustomServerSettings.CustomServerInfos.Remove(serverInfo);
        logger.LogInformationEx($"User remove custom server info, ep:{serverInfo.EndPointDescription}");
    }

    [RelayCommand]
    private void RuleTutorial()
    {
        //todo
    }

    private async Task SaveSettingInternal<T>(T obj)
    {
        await persistence.Save(obj);
        logger.LogInformationEx($"setting {typeof(T).Name} has been saved.");
    }

    private async Task<T> ResetSettingInternal<T>() where T : new()
    {
        var newSetting = new T();
        await persistence.Save(newSetting);
        logger.LogInformationEx($"setting {typeof(T).Name} has been reset.");
        return await persistence.Load<T>();
    }

    [RelayCommand]
    private void PlayNotificationSound()
    {
        applicationNotification.PlaySound();
    }

    [RelayCommand]
    private async Task SaveSetting()
    {
        await SaveSettingInternal(ApplicationSettings);
        await SaveSettingInternal(FysServerSettings);
        await SaveSettingInternal(CustomServerSettings);
        await dialogManager.ShowMessageDialog("选项已保存！");
    }

    [RelayCommand]
    private void DeleteMapSubscribe(MapSubscribe mapSubscribe)
    {
        ApplicationSettings.UserMapSubscribes.Remove(mapSubscribe);
        logger.LogInformationEx($"delete map subscribe:{mapSubscribe.Name}");
    }

    [RelayCommand]
    private async Task ResetSetting()
    {
        if (!await dialogManager.ShowComfirmDialog("是否重置选项？"))
            return;
        ApplicationSettings = await ResetSettingInternal<ApplicationSettings>();
        FysServerSettings = await ResetSettingInternal<FysServerSettings>();
        CustomServerSettings = await ResetSettingInternal<CustomServerSettings>();
        await dialogManager.ShowMessageDialog("选项已被重置！");
    }

    [RelayCommand]
    private async Task TestFysPlayerName()
    {
        if (string.IsNullOrWhiteSpace(FysServerSettings.PlayerName))
        {
            await dialogManager.ShowMessageDialog("请先填写玩家名", DialogMessageType.Error);
            return;
        }

        if (await dialogManager.ShowComfirmDialog("此检查功能通过玩家是否在服务器列表来完成验证，因此需要您先随便进一个FYS的服务器。如果你已经进了服务器，那么就可以开始了。",
                "我准备好了！"))
        {
            FysTestUserNameResult = "正在检查...";
            var checkResult = await fysServerService.CheckUserInvaild(FysServerSettings.PlayerName);
            FysTestUserNameResult = checkResult ? "名称检查成功" : "名称检查失败，请确认名称是否打全打对，或者是否已经进了服务器";
        }
    }
}