using System.Text.Json.Serialization;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.EXG.Bases;

public class EXGServerStatus
{
    [JsonPropertyName("Server")]
    public EXGServer Server { get; set; }
    
    [JsonPropertyName("Status")]
    public EXGStatus Status { get; set; }
}