using CommunityToolkit.Mvvm.ComponentModel;

namespace DpMapSubscribeTool.Services.Persistences;

public partial class FysServerSettings : ObservableObject
{
    [ObservableProperty]
    private string playerName = string.Empty;
    
    [ObservableProperty]
    private int eventDataRefreshInterval = 30;
}