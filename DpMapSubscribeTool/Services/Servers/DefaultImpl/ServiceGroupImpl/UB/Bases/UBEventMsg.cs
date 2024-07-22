using System.Text.Json;
using System.Text.Json.Serialization;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.UB.Bases;

public class UBEventMsg
{
    [JsonPropertyName("client")]
    public int ClientId { get; set; }

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    [JsonPropertyName("event")]
    public string Event { get; set; }

    [JsonPropertyName("server")]
    public int ServerId { get; set; }

    public T GetDataAs<T>()
    {
        return Data.Deserialize<T>() ?? default;
    }
}