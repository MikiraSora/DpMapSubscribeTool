using CommunityToolkit.Mvvm.ComponentModel;

namespace DpMapSubscribeTool.Models;

public partial class ServerGroupFilter : ObservableObject
{
    [ObservableProperty]
    private bool isEnable;

    [ObservableProperty]
    private string serviceGroup;

    [ObservableProperty]
    private string serviceGroupDescription;
}