using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Notifications;
using DpMapSubscribeTool.Services.Servers;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.MethodExtensions;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.ViewModels.Pages.ServerList;

public partial class ServerListPageViewModel : PageViewModelBase
{
    private readonly IApplicationNotification applicationNotification;
    private readonly ILogger<ServerListPageViewModel> logger;

    [ObservableProperty]
    private IServerManager serverManager;

    public ServerListPageViewModel()
    {
        DesignModeHelper.CheckOnlyForDesignMode();
    }

    public ServerListPageViewModel(ILogger<ServerListPageViewModel> logger,
        IApplicationNotification applicationNotification, IServerManager serverManager)
    {
        this.logger = logger;
        this.applicationNotification = applicationNotification;
        this.serverManager = serverManager;
    }

    public override string Title => "服务器列表";

    [RelayCommand]
    private void ShowToast(Server server)
    {
        applicationNotification.NofityServerForSubscribeMap(server, default,
            () => ServerManager.JoinServer(server).NoWait());
    }
}