using System.Text.Json.Serialization;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.ZED.Bases;

public class ZEDServer
{
    [JsonPropertyName("Protocol")]
    public int Protocol { get; set; }

    [JsonPropertyName("HostName")]
    public string HostName { get; set; }

    [JsonPropertyName("Map")]
    public string Map { get; set; }

    [JsonPropertyName("ModDir")]
    public string ModDir { get; set; }

    [JsonPropertyName("ModDesc")]
    public string ModDesc { get; set; }

    [JsonPropertyName("AppID")]
    public int AppID { get; set; }

    [JsonPropertyName("Players")]
    public int Players { get; set; }

    [JsonPropertyName("MaxPlayers")]
    public int MaxPlayers { get; set; }

    [JsonPropertyName("Bots")]
    public int Bots { get; set; }

    [JsonPropertyName("Dedicated")]
    public string Dedicated { get; set; }

    [JsonPropertyName("Os")]
    public string Os { get; set; }

    [JsonPropertyName("Password")]
    public bool Password { get; set; }

    [JsonPropertyName("Secure")]
    public bool Secure { get; set; }

    [JsonPropertyName("Version")]
    public string Version { get; set; }

    [JsonPropertyName("ExtraDataFlags")]
    public int ExtraDataFlags { get; set; }

    [JsonPropertyName("GamePort")]
    public int GamePort { get; set; }

    [JsonPropertyName("SteamID")]
    public ulong SteamID { get; set; }

    [JsonPropertyName("GameTags")]
    public string GameTags { get; set; }

    [JsonPropertyName("GameID")]
    public int GameID { get; set; }

    [JsonPropertyName("ip")]
    public string Ip { get; set; }

    [JsonPropertyName("MapChi")]
    public string MapChi { get; set; }
}