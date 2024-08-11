using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Dialog;
using DpMapSubscribeTool.Services.Notifications;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Services.Servers;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.MethodExtensions;
using DpMapSubscribeTool.ViewModels.Dialogs.SqueezeJoinSetup;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.ViewModels.Pages.ServerList;

public partial class ServerListPageViewModel : PageViewModelBase
{
    private readonly IApplicationNotification applicationNotification;

    private readonly IDialogManager dialogManager;
    private readonly ILogger<ServerListPageViewModel> logger;
    private readonly IPersistence persistence;

    [ObservableProperty]
    private bool isShowFilterPane;

    [ObservableProperty]
    private bool isSplitPaneOpen;

    [ObservableProperty]
    private ServerInfomationDetail serverDetail;

    [ObservableProperty]
    private IServerManager serverManager;

    public ServerListPageViewModel()
    {
        DesignModeHelper.CheckOnlyForDesignMode();
    }

    public ServerListPageViewModel(ILogger<ServerListPageViewModel> logger, IPersistence persistence,
        IApplicationNotification applicationNotification, IServerManager serverManager, IDialogManager dialogManager)
    {
        this.logger = logger;
        this.persistence = persistence;
        this.applicationNotification = applicationNotification;
        this.serverManager = serverManager;
        this.dialogManager = dialogManager;
    }

    public override string Title => "服务器列表";

    [RelayCommand]
    private void ShowToast(Server server)
    {
        applicationNotification.NofitySqueezeJoinSuccess(server.Info).NoWait();
    }

    [RelayCommand]
    private async Task JoinServer(Server server)
    {
        await ServerManager.JoinServer(server);
    }

    [RelayCommand]
    private void ShowFilterPane()
    {
        IsShowFilterPane = true;
        IsSplitPaneOpen = true;
    }

    [RelayCommand]
    private async Task ShowServerInfoPane(Server server)
    {
        IsShowFilterPane = false;
        IsSplitPaneOpen = true;
        ServerDetail = await ServerManager.BeginServerInfomationDetailQuery(server.Info);
    }

    [RelayCommand]
    private async Task RefreshFilterServerList()
    {
        await ServerManager.RefreshFilterServers();
    }

    [RelayCommand]
    private async Task ResetServerListFilterOptions()
    {
        await ServerManager.ResetServerListFilterOptions();
    }

    [RelayCommand]
    private async Task PaneClosed()
    {
        IsSplitPaneOpen = false;
        await ServerManager.StopServerInfomationDetailQuery();
    }

    [RelayCommand]
    private async Task ServerDoubleTapped(Server server)
    {
        await ShowServerInfoPane(server);
    }

    [RelayCommand]
    private async Task SqueezeJoinServer(Server server)
    {
        var setupModel = await dialogManager.ShowDialog<SqueezeJoinSetupDialogViewModel>();
        var option = setupModel.Option;
        if (setupModel.IsComfirm)
            //start squeeze-join
            await ServerManager.SqueezeJoinServer(server, option);
    }

    [RelayCommand]
    private async Task RefreshServers()
    {
        await ServerManager.RefreshServers();
    }
}