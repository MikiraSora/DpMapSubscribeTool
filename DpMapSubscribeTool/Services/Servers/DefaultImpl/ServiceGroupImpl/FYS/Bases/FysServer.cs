using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Utils;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.FYS.Bases;

public class FysServer : Server
{
    private readonly ILogger<FysServer> logger;

    public FysServer(ILogger<FysServer> logger)
    {
        this.logger = logger;
    }

    public void UpdateProperties(EventStreamServer fromServer, ServerInfo info)
    {
        Info = info;
        UpdateProperties(fromServer);
    }

    public void UpdateProperties(EventStreamServer fromServer)
    {
        Map = fromServer.Map;
        MaxPlayerCount = fromServer.MaxPlayers;
        CurrentPlayerCount = fromServer.CurrentPlayers;

        State = fromServer.TotalStage > 0 ? $"{fromServer.CurrentStage}/{fromServer.TotalStage}" : string.Empty;
    }
}