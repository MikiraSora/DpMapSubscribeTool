using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Controls;

public partial class ServerList : UserControl
{
    public static readonly StyledProperty<ObservableCollection<Server>> ServersProperty =
        AvaloniaProperty.Register<ServerList, ObservableCollection<Server>>(
            nameof(Servers));

    public static readonly StyledProperty<ICommand> JoinServerCommandProperty =
        AvaloniaProperty.Register<ServerList, ICommand>(nameof(JoinServerCommand), enableDataValidation: true);

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
}