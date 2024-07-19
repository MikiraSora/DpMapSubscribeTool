using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DpMapSubscribeTool.Services.Map.DefaultImpl;

public partial class TranslationMapData : ObservableObject
{
    [ObservableProperty]
    private Dictionary<string, string> translationNames = new();
}