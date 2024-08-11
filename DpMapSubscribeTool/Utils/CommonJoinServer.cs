using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DpMapSubscribeTool.Services.Dialog;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Utils;

[RegisterInjectable(typeof(CommonJoinServer))]
public class CommonJoinServer
{
    private readonly IDialogManager dialogManager;
    private readonly ILogger<CommonJoinServer> logger;

    public CommonJoinServer(ILogger<CommonJoinServer> logger, IDialogManager dialogManager)
    {
        this.logger = logger;
        this.dialogManager = dialogManager;
    }

    public async Task JoinServer(string host, int port)
    {
        var ipList = await Dns.GetHostAddressesAsync(host);
        if (ipList.Length == 0)
        {
            logger.LogWarningEx($"Can't lookup ip address for host name:{host}");
            await dialogManager.ShowMessageDialog($"无法进入服务器，无法获取服务器ip地址({host})", DialogMessageType.Error);
            return;
        }

        var protocal = isRunningSteamChina() ? "steamchina" : "steam";

        var ipAddr = ipList.First();
        //var steamCmd = $"steam://connect/{ipAddr}:{serverInfo.Port}";
        var steamCmd = $"{protocal}://rungame/730/76561202255233023/+connect%20{ipAddr}:{port}";
        logger.LogInformationEx($"execute steamCmd: {steamCmd}");
        Process.Start(new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = true,
            Arguments = $"/C start {steamCmd}",
            CreateNoWindow = true
        });
    }

    public bool isRunningSteamChina()
    {
        return Process.GetProcessesByName("steamchina").Length > 0;
    }
}