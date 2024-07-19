using System.Threading.Tasks;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Services.Servers;

public interface IServerActionExecutor:IServerServiceBase
{
    Task Join(ServerInfo serverInfo);

    Task Join(Server server)
    {
        return Join(server.Info);
    }
}