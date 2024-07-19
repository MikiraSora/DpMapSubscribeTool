using System.Collections.Generic;
using System.Threading.Tasks;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Services.Servers;

public interface IServerInfoSearcher : IServerServiceBase
{
    Task<IEnumerable<ServerInfo>> GetAllAvaliableServerInfo();
}