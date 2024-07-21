using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DpMapSubscribeTool.Models;

public partial class ServerListFilterOptions : ObservableObject
{
    [ObservableProperty]
    private int filterDelay;

    [ObservableProperty]
    private string filterKeyword;

    [ObservableProperty]
    private int filterPlayerCountRemain;

    [ObservableProperty]
    private bool isEnable;

    [ObservableProperty]
    private bool isEnableDelayFilter;

    [ObservableProperty]
    private bool isEnableKeywordFilter;

    [ObservableProperty]
    private bool isEnablePlayerCountRemainFilter;

    [ObservableProperty]
    private bool isEnableServerGroupFilter;

    [ObservableProperty]
    private ObservableCollection<ServerGroupFilter> serverGroupFilters = new();
}