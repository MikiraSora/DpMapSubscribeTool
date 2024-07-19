using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.Test;

public interface ITestServerManager
{
    Server ServerWaitForUpdate { get; set; }
}