using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using DpMapSubscribeTool.Models;
using DynamicData;
using DynamicData.Binding;

namespace DpMapSubscribeTool.Controls;

public partial class ServerList : UserControl
{
    public static readonly StyledProperty<ObservableCollection<Server>> ServersProperty =
        AvaloniaProperty.Register<ServerList, ObservableCollection<Server>>(nameof(Servers));

    public static readonly StyledProperty<ReadOnlyObservableCollection<Server>> ServersViewProperty =
        AvaloniaProperty.Register<ServerList, ReadOnlyObservableCollection<Server>>(nameof(ServersView));

    public static readonly StyledProperty<ICommand> JoinServerCommandProperty =
        AvaloniaProperty.Register<ServerList, ICommand>(nameof(JoinServerCommand), enableDataValidation: true);

    public static readonly StyledProperty<SortDirection> MapTranslationNameSortDirectionProperty =
        AvaloniaProperty.Register<ServerList, SortDirection>(nameof(MapTranslationNameSortDirection),
            enableDataValidation: true);

    public static readonly StyledProperty<SortDirection> MapSortDirectionProperty =
        AvaloniaProperty.Register<ServerList, SortDirection>(nameof(MapSortDirection), enableDataValidation: true);
    
    public static readonly StyledProperty<SortDirection> GameModeSortDirectionProperty =
        AvaloniaProperty.Register<ServerList, SortDirection>(nameof(GameModeSortDirection), enableDataValidation: true);

    public static readonly StyledProperty<SortDirection> NameSortDirectionProperty =
        AvaloniaProperty.Register<ServerList, SortDirection>(nameof(NameSortDirection), enableDataValidation: true);

    public static readonly StyledProperty<SortDirection> PlayCountSortDirectionProperty =
        AvaloniaProperty.Register<ServerList, SortDirection>(nameof(PlayCountSortDirection),
            enableDataValidation: true);

    public static readonly StyledProperty<SortDirection> DelaySortDirectionProperty =
        AvaloniaProperty.Register<ServerList, SortDirection>(nameof(DelaySortDirection), enableDataValidation: true);

    public static readonly StyledProperty<ICommand> ServerDoubleTappedCommandProperty =
        AvaloniaProperty.Register<ServerList, ICommand>(nameof(ServerDoubleTappedCommand), enableDataValidation: true);

    public static readonly StyledProperty<ICommand> SqueezeJoinServerCommandProperty =
        AvaloniaProperty.Register<ServerList, ICommand>(nameof(SqueezeJoinServerCommand), enableDataValidation: true);

    private readonly Subject<IComparer<Server>> _comparerSubject;

    private bool _isSortingEnabled;
    private IDisposable _subscription;

    public ServerList()
    {
        InitializeComponent();
        _comparerSubject = new Subject<IComparer<Server>>();
        hostDataGrid.DataContext = this;

        PropertyChanged += OnLocalChanged;
    }

    public SortDirection GameModeSortDirection
    {
        get => GetValue(GameModeSortDirectionProperty);
        set => SetValue(GameModeSortDirectionProperty, value);
    }

    public SortDirection PlayCountSortDirection
    {
        get => GetValue(PlayCountSortDirectionProperty);
        set => SetValue(PlayCountSortDirectionProperty, value);
    }

    public SortDirection DelaySortDirection
    {
        get => GetValue(DelaySortDirectionProperty);
        set => SetValue(DelaySortDirectionProperty, value);
    }

    public SortDirection MapSortDirection
    {
        get => GetValue(MapSortDirectionProperty);
        set => SetValue(MapSortDirectionProperty, value);
    }

    public SortDirection MapTranslationNameSortDirection
    {
        get => GetValue(MapTranslationNameSortDirectionProperty);
        set => SetValue(MapTranslationNameSortDirectionProperty, value);
    }

    public SortDirection NameSortDirection
    {
        get => GetValue(NameSortDirectionProperty);
        set => SetValue(NameSortDirectionProperty, value);
    }

    public ObservableCollection<Server> Servers
    {
        get => GetValue(ServersProperty);
        set => SetValue(ServersProperty, value);
    }

    public ReadOnlyObservableCollection<Server> ServersView
    {
        get => GetValue(ServersViewProperty);
        set => SetValue(ServersViewProperty, value);
    }

    public ICommand ServerDoubleTappedCommand
    {
        get => GetValue(ServerDoubleTappedCommandProperty);
        set => SetValue(ServerDoubleTappedCommandProperty, value);
    }

    public ICommand JoinServerCommand
    {
        get => GetValue(JoinServerCommandProperty);
        set => SetValue(JoinServerCommandProperty, value);
    }

    public ICommand SqueezeJoinServerCommand
    {
        get => GetValue(SqueezeJoinServerCommandProperty);
        set => SetValue(SqueezeJoinServerCommandProperty, value);
    }

    private void OnLocalChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ServersProperty)
            if (ServersView == null)
                CleanSort();
    }

    private void HostDataGrid_OnDoubleTapped(object sender, TappedEventArgs e)
    {
        if (hostDataGrid.SelectedItem is not Server server)
            return;
        if (!(ServerDoubleTappedCommand?.CanExecute(server) ?? false))
            return;
        ServerDoubleTappedCommand?.Execute(server);
    }

    private void CleanSort()
    {
        _subscription?.Dispose();
        _subscription = GetDefaultObservable().Subscribe();
        _isSortingEnabled = false;
    }

    private IObservable<IChangeSet<Server>> GetSortObservable(IComparer<Server> comparer)
    {
        var r = Servers.ToObservableChangeSet()
            .AutoRefresh(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200))
            .Sort(comparer, comparerChanged: _comparerSubject)
            .Bind(out var _items);
        ServersView = _items;
        return r;
    }

    private IObservable<IChangeSet<Server>> GetDefaultObservable()
    {
        var r = Servers.ToObservableChangeSet()
            .AutoRefresh()
            .Bind(out var _items);
        ServersView = _items;
        return r;
    }

    private void EnableSort(Func<Server, IComparable> expression, StyledProperty<SortDirection> sortProperty)
    {
        CleanSort();

        var sortDirection = GetValue(sortProperty) switch
        {
            SortDirection.Ascending => SortDirection.Descending,
            SortDirection.Descending => SortDirection.Ascending,
            _ => throw new ArgumentOutOfRangeException()
        };

        SetValue(sortProperty, sortDirection);

        var sortExpressionComparer = sortDirection == SortDirection.Ascending
            ? SortExpressionComparer<Server>.Ascending(expression)
            : SortExpressionComparer<Server>.Descending(expression);

        if (!_isSortingEnabled)
        {
            _subscription?.Dispose();
            _subscription = GetSortObservable(sortExpressionComparer)
                .Subscribe();
            _isSortingEnabled = true;
        }
        else
        {
            _comparerSubject.OnNext(sortExpressionComparer);
        }
    }

    [RelayCommand]
    private void Sort(string sortMemberPath)
    {
        switch (sortMemberPath)
        {
            case nameof(Server.Map):
                EnableSort(x => x.Map, MapSortDirectionProperty);
                break;
            case nameof(Server.Info.Name):
                EnableSort(x => x.Info.Name, NameSortDirectionProperty);
                break;
            case nameof(Server.Delay):
                EnableSort(x => x.Delay, DelaySortDirectionProperty);
                break;
            case nameof(Server.CurrentPlayerCount):
                EnableSort(x => x.CurrentPlayerCount, PlayCountSortDirectionProperty);
                break;
            case nameof(Server.GameMode):
                EnableSort(x => x.GameMode, GameModeSortDirectionProperty);
                break;
            case nameof(Server.MapTranslationName):
                EnableSort(x => x.MapTranslationName, MapTranslationNameSortDirectionProperty);
                break;
            default:
                CleanSort();
                break;
        }
    }
}