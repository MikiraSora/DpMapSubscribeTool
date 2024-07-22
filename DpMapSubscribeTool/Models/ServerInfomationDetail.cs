using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DpMapSubscribeTool.Models;

public partial class ServerInfomationDetail: ObservableObject
{
    [ObservableProperty]
    private Server server;

    [ObservableProperty]
    private ObservableCollection<ServerInfomationPlayerDetail> playerDetails;
}