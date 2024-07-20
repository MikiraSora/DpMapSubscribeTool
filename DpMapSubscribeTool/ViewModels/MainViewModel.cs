using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Controls;
using DpMapSubscribeTool.ViewModels.Pages;
using System.Linq;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Services.Notifications;
using DpMapSubscribeTool.Services.Servers;
using DpMapSubscribeTool.ViewModels.Pages.Home;
using DpMapSubscribeTool.ViewModels.Pages.ServerList;
using DpMapSubscribeTool.ViewModels.Pages.Setting;

namespace DpMapSubscribeTool.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> logger;
    private readonly IPersistence persistence;
    private readonly IServerManager serverManager;
    private readonly ViewModelFactory viewModelFactory;

    [ObservableProperty]
    private bool enableNavigratable;

    [ObservableProperty]
    private bool isPaneOpen;

    [ObservableProperty]
    private ViewModelBase mainPageContent;

    [ObservableProperty]
    private ListItemTemplate selectedListItem;

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
        new ListItemTemplate(typeof(ServerListPageViewModel), "服务器列表", "ServerList"),
        new ListItemTemplate(typeof(SettingPageViewModel), "设置", "Setting"),
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
        await serverManager.PrepareData();
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
}
