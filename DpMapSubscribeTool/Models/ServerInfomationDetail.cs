using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DpMapSubscribeTool.Models;

public partial class ServerInfomationDetail : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ServerInfomationPlayerDetail> playerDetails = new();

    [ObservableProperty]
    private Server server;
}