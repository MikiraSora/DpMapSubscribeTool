using System;
using System.Linq;
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

    [NotifyPropertyChangedFor(nameof(GameMode))]
    [NotifyPropertyChangedFor(nameof(GameModeDescription))]
    [ObservableProperty]
    private string map;
    
    [ObservableProperty]
    private string mapTranslationName;

    [ObservableProperty]
    private int maxPlayerCount;

    [ObservableProperty]
    private string state;

    public GameMode GameMode =>
        Enum.TryParse<GameMode>((Map ?? string.Empty).Split("_").FirstOrDefault(), true, out var mode)
            ? mode
            : GameMode.Unknown;

    public string GameModeDescription =>
        GameMode switch
        {
            GameMode.ZE => "僵尸逃跑",
            GameMode.ZM => "僵尸感染",
            GameMode.DR => "死亡奔跑",
            GameMode.JB => "越狱",
            GameMode.BHOP => "连跳",
            GameMode.KZ => "身法",
            GameMode.SURF => "滑翔",
            GameMode.AS => "暗杀",
            GameMode.TTT => "小镇谍影",
            GameMode.MG => "迷你小游戏",
            _ => string.Empty
        };

    public int Id { get; } = genId++;

    public override string ToString()
    {
        return $"{Id} {Info}";
    }
}