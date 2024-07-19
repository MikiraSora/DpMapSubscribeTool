using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.FYS.Bases;

public class FysServer : Server
{
    public void UpdateProperties(EventStreamServer fromServer, ServerInfo info)
    {
        Info = info;
        UpdateProperties(fromServer);
    }

    public void UpdateProperties(EventStreamServer fromServer)
    {
        CurrentPlayerCount = fromServer.CurrentPlayers;
        Map = fromServer.Map;
        MaxPlayerCount = fromServer.MaxPlayers;

        State = fromServer.TotalStage > 0 ? $"{fromServer.CurrentStage}/{fromServer.TotalStage}" : string.Empty;
    }
}