using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Controls;

public partial class ServerList : UserControl
{
    public static readonly StyledProperty<ObservableCollection<Server>> ServersProperty =
        AvaloniaProperty.Register<ServerList, ObservableCollection<Server>>(
            nameof(Servers));


    public static readonly StyledProperty<ICommand> JoinServerCommandProperty =
        AvaloniaProperty.Register<ServerList, ICommand>(nameof(JoinServerCommand), enableDataValidation: true);

    public static readonly StyledProperty<ICommand> ServerDoubleTappedCommandProperty =
        AvaloniaProperty.Register<ServerList, ICommand>(nameof(ServerDoubleTappedCommand), enableDataValidation: true);

    public static readonly StyledProperty<ICommand> SqueezeJoinServerCommandProperty =
        AvaloniaProperty.Register<ServerList, ICommand>(nameof(SqueezeJoinServerCommand), enableDataValidation: true);

    public ServerList()
    {
        InitializeComponent();
        hostDataGrid.DataContext = this;
    }

    public ObservableCollection<Server> Servers
    {
        get => GetValue(ServersProperty);
        set => SetValue(ServersProperty, value);
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

    private void HostDataGrid_OnDoubleTapped(object sender, TappedEventArgs e)
    {
        if (hostDataGrid.SelectedItem is not Server server)
            return;
        if (!(ServerDoubleTappedCommand?.CanExecute(server) ?? false))
            return;
        ServerDoubleTappedCommand?.Execute(server);
    }
}