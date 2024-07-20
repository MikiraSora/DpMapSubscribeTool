using CommunityToolkit.Mvvm.ComponentModel;

namespace DpMapSubscribeTool.Models;

public partial class SqueezeJoinServerOption : ObservableObject
{
    [ObservableProperty]
    private bool makeGameForegroundIfSuccess;

    [ObservableProperty]
    private bool notifyIfSuccess = true;

    //value can't not be <= 0.
    //IServerManager will auto join server when CurrentPlayerCount <= (MaxPlayerCount - SqueezeTargetPlayerCountDiff)
    [ObservableProperty]
    private int squeezeTargetPlayerCountDiff = 4;

    [ObservableProperty]
    private int tryJoinInterval = 10;
}