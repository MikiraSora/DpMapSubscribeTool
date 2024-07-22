using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DpMapSubscribeTool.Models;

public partial class ServerInfomationPlayerDetail : ObservableObject
{
    [ObservableProperty]
    private TimeSpan duration;

    [ObservableProperty]
    private string name;
    
    [ObservableProperty]
    private long score;
}