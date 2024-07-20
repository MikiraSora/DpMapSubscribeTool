using System.Threading.Tasks;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.FYS;

public interface IFysServerServiceBase : IServerServiceBase
{
    string IServerServiceBase.ServerGroup => "FYS";

    Task<bool> CheckUserInvaild(string name);
}