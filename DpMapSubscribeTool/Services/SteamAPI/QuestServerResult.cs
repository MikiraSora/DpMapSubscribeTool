namespace DpMapSubscribeTool.Services.SteamAPI;

public class QuestServerResult
{
    public string Map { get; set; }
    public int CurrentPlayerCount { get; set; }
    public int MaxPlayerCount { get; set; }
    public int Delay { get; set; }
    public string Name { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
}