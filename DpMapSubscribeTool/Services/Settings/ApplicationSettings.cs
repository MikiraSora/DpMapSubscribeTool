using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Services.Settings;

public partial class ApplicationSettings : ObservableObject, ISetting
{
    [ObservableProperty]
    private bool enableNotication = true;

    [ObservableProperty]
    private bool enableNoticationBySound = true;
    
    [ObservableProperty]
    private int autoPingTimeInterval = 15;
    
    [ObservableProperty]
    private int autoRefreshTimeInterval = 30;

    [ObservableProperty]
    private bool enableNoticationByTaskbar = true;

    [ObservableProperty]
    private string noticationSoundFilePath;
    
    [ObservableProperty]
    private bool enableNoticationIfGameForeground;

    [ObservableProperty]
    private ObservableCollection<MapSubscribe> userMapSubscribes = new();
}