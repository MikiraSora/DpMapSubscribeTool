using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Services.Servers;

public interface IServerManager
{
    ServerListFilterOptions CurrentServerListFilterOptions { get; }
    SqueezeJoinTaskStatus CurrentSqueezeJoinTaskStatus { get; }

    ObservableCollection<Server> Servers { get; }
    ObservableCollection<Server> FilterServers { get; }
    ObservableCollection<Server> SubscribeServers { get; }
    bool IsDataReady { get; }

    Task UpdateServer(Server server);
    Task PingServer(Server server);

    Task JoinServer(Server server)
    {
        return JoinServer(server.Info);
    }

    Task JoinServer(ServerInfo serverInfo);

    Task PrepareData();

    Task RefreshServers();

    Task SqueezeJoinServer(ServerInfo serverInfo, SqueezeJoinServerOption option);

    Task SqueezeJoinServer(Server server, SqueezeJoinServerOption option)
    {
        return SqueezeJoinServer(server.Info, option);
    }

    Task StopSqueezeJoinServerTask();

    Task RefreshFilterServers();
    Task ResetServerListFilterOptions();
}