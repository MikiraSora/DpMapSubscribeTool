using System.Text.Json.Serialization;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.UB.Bases;

public class UBLevel
{
    [JsonPropertyName("num")]
    public int Num { get; set; }

    [JsonPropertyName("max")]
    public int Max { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}