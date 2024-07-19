using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.FYS.Bases;

public class EventStreamData
{
    [JsonPropertyName("servers")]
    public List<EventStreamServer> Servers { get; set; }
}