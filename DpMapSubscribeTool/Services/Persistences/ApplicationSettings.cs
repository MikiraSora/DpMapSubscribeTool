using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Services.Persistences;

public partial class ApplicationSettings : ObservableObject
{
    [ObservableProperty]
    private int autoPingTimeInterval = 15;

    [ObservableProperty]
    private int autoRefreshTimeInterval = 30;

    [ObservableProperty]
    private bool enableNotication = true;

    [ObservableProperty]
    private bool enableNoticationBySound = true;

    [ObservableProperty]
    private bool enableNoticationByTaskbar = true;

    [ObservableProperty]
    private bool enableNoticationIfGameForeground;

    [ObservableProperty]
    private string noticationSoundFilePath;

    [ObservableProperty]
    private ObservableCollection<MapSubscribe> userMapSubscribes = new();
}