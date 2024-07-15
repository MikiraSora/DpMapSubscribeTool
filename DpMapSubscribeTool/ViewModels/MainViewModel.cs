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
using DpMapSubscribeTool.ViewModels.Pages.Home;

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

    public MainViewModel(
        ILogger<MainViewModel> logger,
        IPersistence persistence,
        ViewModelFactory viewModelFactory)
    {
        this.logger = logger;
        this.persistence = persistence;
        this.viewModelFactory = viewModelFactory;

        ProcessInit();
    }

    public ObservableCollection<ListItemTemplate> TopItems { get; } = new()
    {
        new ListItemTemplate(typeof(HomePageViewModel), "主页", "Home"),
        /*
        new ListItemTemplate(typeof(UserInfoPageViewModel), "用户信息", "Person"),
        new ListItemTemplate(typeof(Pages.MaimaiDx.HomePageViewModel), "maimai DX 主页", "Games"),
        new ListItemTemplate(typeof(MusicListPageViewModel), "maimai Dx 曲库", "MusicNote1"),
        new ListItemTemplate(typeof(CollectionLookupPageViewModel), "maimai Dx 收藏品", "CollectionsAdd")
        */
    };

    public ObservableCollection<ListItemTemplate> BottomItems { get; } = new();

    public async ValueTask NavigatePageAsync<T>(T existObj = default) where T : PageViewModelBase
    {
        var obj = existObj ?? viewModelFactory.CreateViewModel(typeof(T));
        var type = obj.GetType();

        MainPageContent = obj;

        if (TopItems.Concat(BottomItems).FirstOrDefault(x => x.ModelType == type) is ListItemTemplate template)
            //make select status if modelView type contains menu list.
            SelectedListItem = template;
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
        NavigatePageAsync<HomePageViewModel>();
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
