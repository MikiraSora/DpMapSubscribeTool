using System.Text.Json.Serialization;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.EXG.Bases;

public class EXGStatus
{
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("Map")]
    public string Map { get; set; }

    [JsonPropertyName("MapDisplayName")]
    public string MapDisplayName { get; set; }

    [JsonPropertyName("MaxPlayers")]
    public int MaxPlayers { get; set; }

    [JsonPropertyName("CurrentPlayers")]
    public int CurrentPlayers { get; set; }

    [JsonPropertyName("IsFromA2S")]
    public bool IsFromA2S { get; set; }
}