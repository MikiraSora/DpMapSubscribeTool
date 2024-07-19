using System.Text.Json.Serialization;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.FYS.Bases;

public class EventStreamServer
{
    [JsonPropertyName("game")]
    public string Game { get; set; }

    [JsonPropertyName("network")]
    public string Network { get; set; }

    [JsonPropertyName("map")]
    public string Map { get; set; }

    [JsonPropertyName("translation")]
    public string Translation { get; set; }

    [JsonPropertyName("currentPlayers")]
    public int CurrentPlayers { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("maxPlayers")]
    public int MaxPlayers { get; set; }

    [JsonPropertyName("host")]
    public string Host { get; set; }

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("mapId")]
    public string MapId { get; set; }

    [JsonPropertyName("currentStage")]
    public int CurrentStage { get; set; }

    [JsonPropertyName("totalStage")]
    public int TotalStage { get; set; }

    [JsonPropertyName("extremeStage")]
    public bool ExtremeStage { get; set; }

    [JsonPropertyName("serverId")]
    public int ServerId { get; set; }

    [JsonPropertyName("modeId")]
    public int ModeId { get; set; }
}