using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Dialog;
using DpMapSubscribeTool.Services.Notifications;
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

    [ObservableProperty]
    private IServerManager serverManager;

    public ServerListPageViewModel()
    {
        DesignModeHelper.CheckOnlyForDesignMode();
    }

    public ServerListPageViewModel(ILogger<ServerListPageViewModel> logger,
        IApplicationNotification applicationNotification, IServerManager serverManager, IDialogManager dialogManager)
    {
        this.logger = logger;
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
    private async Task OpenFilterPanel()
    {
        
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