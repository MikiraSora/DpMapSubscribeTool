using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Services.Servers;

public interface IServerSqueezeJoinRunner : IServerServiceBase
{
    Task<bool> CheckSqueezeJoinServer(ServerInfo serverInfo, SqueezeJoinServerOption option,
        CancellationToken cancellationToken);

    Task<bool> IsUserInServer(List<string> playerNameList, CancellationToken cancellationToken);
}