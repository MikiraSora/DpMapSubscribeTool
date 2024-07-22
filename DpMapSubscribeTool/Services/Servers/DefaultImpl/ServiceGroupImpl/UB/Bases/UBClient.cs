using System.Text.Json.Serialization;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.UB.Bases;

public class UBClient
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("steam2")]
    public string Steam2 { get; set; }

    [JsonPropertyName("steam64")]
    public string Steam64 { get; set; }

    [JsonPropertyName("alive")]
    public bool Alive { get; set; }

    [JsonPropertyName("team")]
    public int Team { get; set; }
}