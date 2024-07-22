using System.Text.Json.Serialization;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.EXG.Bases;

public class EXGServer
{
    [JsonPropertyName("Id")]
    public string Id { get; set; }

    [JsonPropertyName("Sort")]
    public int Sort { get; set; }

    [JsonPropertyName("Ip")]
    public string Ip { get; set; }

    [JsonPropertyName("Port")]
    public int Port { get; set; }

    [JsonPropertyName("IsPublic")]
    public bool IsPublic { get; set; }

    [JsonPropertyName("DisplayName")]
    public string DisplayName { get; set; }

    [JsonPropertyName("DisplayNameCN")]
    public string DisplayNameCN { get; set; }

    [JsonPropertyName("WorkshopId")]
    public long WorkshopId { get; set; }

    [JsonPropertyName("Category")]
    public string Category { get; set; }

    [JsonPropertyName("ViewLogRole")]
    public string ViewLogRole { get; set; }
}