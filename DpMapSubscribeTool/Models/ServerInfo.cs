using CommunityToolkit.Mvvm.ComponentModel;

namespace DpMapSubscribeTool.Models;

public partial class ServerInfo : ObservableObject
{
    [ObservableProperty]
    private string host;

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private int port;

    [ObservableProperty]
    private string serverGroup;
    
    [ObservableProperty]
    private string serverGroupDisplay;

    public string EndPointDescription => $"{Host}:{Port}";

    public override string ToString()
    {
        return $"[{ServerGroupDisplay}]{Name}";
    }
}