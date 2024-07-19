using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Services.Servers;

public interface IServerManager
{
    ObservableCollection<Server> Servers { get; }
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
}