namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.FYS;

public interface IFysServerServiceBase : IServerServiceBase
{
    string IServerServiceBase.ServerGroup => "FYS";
}