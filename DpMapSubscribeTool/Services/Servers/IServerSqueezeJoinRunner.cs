using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Services.Servers;

public interface IServerSqueezeJoinRunner : IServerServiceBase
{
    /// <summary>
    ///     check if runner is able to squeeze-join server by specify option and settings.
    /// </summary>
    /// <param name="serverInfo"></param>
    /// <param name="option"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> CheckSqueezeJoinServer(ServerInfo serverInfo, SqueezeJoinServerOption option,
        CancellationToken cancellationToken);

    /// <summary>
    ///     check if user is in their servers.
    /// </summary>
    /// <param name="playerNameList"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> IsUserInServer(List<string> playerNameList, CancellationToken cancellationToken);
}