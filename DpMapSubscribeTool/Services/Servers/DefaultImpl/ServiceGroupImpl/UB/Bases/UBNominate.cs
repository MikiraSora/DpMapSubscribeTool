using System.Text.Json.Serialization;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.UB.Bases;

public class UBNominate
{
    [JsonPropertyName("flag")]
    public int Flag { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("steam64")]
    public string Steam64 { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }
}