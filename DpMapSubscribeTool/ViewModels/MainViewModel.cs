using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DpMapSubscribeTool.Controls;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Services.Servers;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.ViewModels.Pages;
using DpMapSubscribeTool.ViewModels.Pages.Home;
using DpMapSubscribeTool.ViewModels.Pages.ServerList;
using DpMapSubscribeTool.ViewModels.Pages.Setting;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> logger;
    private readonly IPersistence persistence;

    private readonly ViewModelFactory viewModelFactory;

    [ObservableProperty]
    private bool enableNavigratable;

    [ObservableProperty]
    private bool isPaneOpen;

    [ObservableProperty]
    private ViewModelBase mainPageContent;

    [ObservableProperty]
    private ListItemTemplate selectedListItem;

    [ObservableProperty]
    private IServerManager serverManager;

    public MainViewModel(
        ILogger<MainViewModel> logger,
        IPersistence persistence,
        IServerManager serverManager,
        ViewModelFactory viewModelFactory)
    {
        this.logger = logger;
        this.persistence = persistence;
        this.serverManager = serverManager;
        this.viewModelFactory = viewModelFactory;

        ProcessInit();
    }

    public ObservableCollection<ListItemTemplate> TopItems { get; } = new()
    {
        new ListItemTemplate(typeof(HomePageViewModel), "主页", "Home"),
        new ListItemTemplate(typeof(ServerListPageViewModel), "服务器列表", "DataBarHorizontal"),
        new ListItemTemplate(typeof(SettingPageViewModel), "设置", "Settings")
    };

    public ObservableCollection<ListItemTemplate> BottomItems { get; } = new();

    public ValueTask NavigatePageAsync<T>(T existObj = default) where T : PageViewModelBase
    {
        var obj = existObj ?? viewModelFactory.CreateViewModel(typeof(T));
        var type = obj.GetType();

        MainPageContent = obj;

        if (TopItems.Concat(BottomItems).FirstOrDefault(x => x.ModelType == type) is ListItemTemplate template)
            //make select status if modelView type contains menu list.
            SelectedListItem = template;

        return ValueTask.CompletedTask;
    }

    public ValueTask NavigatePageAsync(Type pageViewModelType)
    {
        return NavigatePageAsync(viewModelFactory.CreateViewModel(pageViewModelType) as PageViewModelBase);
    }

    private async void ProcessInit()
    {
        if (!DesignModeHelper.IsDesignMode)
        {
            var setting = await persistence.Load<ApplicationSettings>();

            //using var disp = notification.BeginLoadingNotification("自动登录中", out var cancellationToken);
        }

        //load default page.
        await NavigatePageAsync<HomePageViewModel>();
        //prepare server list
        await ServerManager.PrepareData();
    }

    partial void OnSelectedListItemChanged(ListItemTemplate oldValue, ListItemTemplate newValue)
    {
        if (MainPageContent?.GetType() != newValue?.ModelType)
            MainPageContent = viewModelFactory.CreateViewModel(newValue?.ModelType);
    }

    [RelayCommand]
    private void TriggerPane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    [RelayCommand]
    private void StopSqueezeJoinServerTask()
    {
        ServerManager.StopSqueezeJoinServerTask();
    }
}