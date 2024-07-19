using System.Threading.Tasks;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Services.Servers;

public interface IServerStateUpdater : IServerServiceBase
{
    Task<Server> QueryServer(ServerInfo info);
    Task UpdateServer(Server server);
}