using CommunityToolkit.Mvvm.ComponentModel;

namespace DpMapSubscribeTool.Models;

public abstract partial class Server : ObservableObject
{
    private static int genId;

    [ObservableProperty]
    private int currentPlayerCount;

    [ObservableProperty]
    private int delay;

    [ObservableProperty]
    private ServerInfo info;

    [ObservableProperty]
    private string map;

    [ObservableProperty]
    private int maxPlayerCount;

    [ObservableProperty]
    private string state;

    public int Id { get; } = genId++;

    public override string ToString()
    {
        return $"{Id} {Info}";
    }
}