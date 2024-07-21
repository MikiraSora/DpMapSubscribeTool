using System.Threading.Tasks;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.FYS;

public interface IFysServerServiceBase : IServerServiceBase
{
    Task<bool> CheckUserInvaild(string name);
}