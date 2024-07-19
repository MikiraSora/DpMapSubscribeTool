using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.MessageBox;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.Test;
using DpMapSubscribeTool.Services.Settings;
using DpMapSubscribeTool.Utils;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.ViewModels.Pages.Setting;

public partial class SettingPageViewModel : PageViewModelBase
{
    private readonly ILogger<SettingPageViewModel> logger;
    private readonly IApplicationMessageBox messageBox;
    private readonly IPersistence persistence;
    private readonly ISettingManager settingManager;
    
    [ObservableProperty]
    private ITestServerManager testServerManager;

    [ObservableProperty]
    private ApplicationSettings applicationSettings;

    public SettingPageViewModel()
    {
        DesignModeHelper.CheckOnlyForDesignMode();
    }

    public SettingPageViewModel(ISettingManager settingManager, ILogger<SettingPageViewModel> logger,
        IPersistence persistence, IApplicationMessageBox messageBox, ITestServerManager testServerManager)
    {
        this.settingManager = settingManager;
        this.logger = logger;
        this.persistence = persistence;
        this.messageBox = messageBox;
        this.testServerManager = testServerManager;

        Initialize();
    }

    public MapSubscribeRule[] MapSubscribeRuleEnums { get; } = Enum.GetValues<MapSubscribeRule>();

    public override string Title => "Setting";

    private async void Initialize()
    {
        ApplicationSettings = await settingManager.GetSetting<ApplicationSettings>();
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
        logger.LogInformation("User add new subscribe rule.");
    }

    [RelayCommand]
    private void RuleTutorial()
    {
        //todo
    }

    [RelayCommand]
    private async Task SaveSetting()
    {
        await settingManager.SaveSetting<ApplicationSettings>();
        logger.LogInformation("ApplicationSettings has been saved.");
        await messageBox.ShowModalDialog("选项已保存！");
    }

    [RelayCommand]
    private async Task ResetSetting()
    {
        if (!await messageBox.ShowComfirmModalDialog("是否重置选项？"))
            return;
        await settingManager.ResetSetting<ApplicationSettings>();
        logger.LogInformation("ApplicationSettings has been reset.");
        ApplicationSettings = await settingManager.GetSetting<ApplicationSettings>();
        await messageBox.ShowModalDialog("选项已被重置！(但还没保存)");
    }
}