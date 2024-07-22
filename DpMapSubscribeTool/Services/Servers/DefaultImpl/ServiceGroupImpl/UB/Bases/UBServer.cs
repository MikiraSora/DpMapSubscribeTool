using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.UB.Bases;

public class UBServer
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("appid")]
    public int AppId { get; set; }

    [JsonPropertyName("host")]
    public string Host { get; set; }

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("mode")]
    public int Mode { get; set; }

    [JsonPropertyName("maxplayers")]
    public int MaxPlayers { get; set; }

    [JsonPropertyName("t_score")]
    public int TScore { get; set; }

    [JsonPropertyName("ct_score")]
    public int CTScore { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("map")]
    public UBMap CurrentMap { get; set; }

    [JsonPropertyName("nextmap")]
    public UBMap NextMap { get; set; }

    [JsonPropertyName("level")]
    public UBLevel Level { get; set; }

    [JsonPropertyName("timeleft")]
    public long TimeLeft { get; set; }

    [JsonPropertyName("numrounds")]
    public int NumRounds { get; set; }

    [JsonPropertyName("maxrounds")]
    public int MaxRounds { get; set; }

    [JsonPropertyName("clients")]
    public List<UBClient> Clients { get; set; }

    [JsonPropertyName("nominate")]
    public List<UBNominate> Nominate { get; set; }
}