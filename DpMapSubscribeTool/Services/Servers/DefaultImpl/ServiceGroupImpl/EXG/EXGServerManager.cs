using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Dialog;
using DpMapSubscribeTool.Services.Map;
using DpMapSubscribeTool.Services.Networks;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.EXG.Bases;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.EXG;

[RegisterInjectable(typeof(IServerActionExecutor), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerInfoSearcher), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerSqueezeJoinRunner), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerStateUpdater), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IEXGServerServiceBase), ServiceLifetime.Singleton)]
public class EXGServerManager : IEXGServerServiceBase, IServerInfoSearcher, IServerStateUpdater,
    IServerSqueezeJoinRunner,
    IServerActionExecutor
{
    private readonly CommonJoinServer commonJoinServer;
    private readonly IDialogManager dialogManager;
    private readonly IApplicationHttpFactory httpFactory;
    private readonly ILogger<EXGServerManager> logger;
    private readonly IMapManager mapManager;

    private readonly IPersistence persistence;

    private CancellationTokenSource cancellationTokenSource;
    private List<EXGServerStatus> currentServerStatusList;
    private Dictionary<string, EXGServerStatus> currentServerStatusMap;

    private Task dataReadyTask;

    public EXGServerManager(IApplicationHttpFactory httpFactory, ILogger<EXGServerManager> logger,
        CommonJoinServer commonJoinServer,
        IMapManager mapManager, IDialogManager dialogManager, IPersistence persistence)
    {
        this.httpFactory = httpFactory;
        this.logger = logger;
        this.commonJoinServer = commonJoinServer;
        this.mapManager = mapManager;
        this.dialogManager = dialogManager;
        this.persistence = persistence;

        Initialize();
    }

    public string ServerGroup => "EXG";
    public string ServerGroupDescription => "EXG";

    public Task Join(ServerInfo serverInfo)
    {
        return commonJoinServer.JoinServer(serverInfo.Host, serverInfo.Port);
    }

    public async Task<IEnumerable<ServerInfo>> GetAllAvaliableServerInfo()
    {
        await CheckOrWaitDataReady();

        return currentServerStatusList.Select(x => new ServerInfo
        {
            ServerGroupDisplay = ServerGroupDescription,
            ServerGroup = ServerGroup,
            Name = x.Server.DisplayName + " - " + x.Server.DisplayNameCN,
            Host = x.Server.Ip,
            Port = x.Server.Port
        });
    }

    public Task<bool> CheckSqueezeJoinServer(ServerInfo serverInfo, SqueezeJoinServerOption option,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task<bool> IsUserInServer(List<string> playerNameList, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Server> QueryServer(ServerInfo info)
    {
        var exgWrappedServer = new EXGWrappedServer
        {
            Info = info
        };

        await UpdateServer(exgWrappedServer);

        return exgWrappedServer;
    }

    public async Task UpdateServer(Server server)
    {
        await CheckOrWaitDataReady();

        if (server is not EXGWrappedServer exgWrappedServer)
        {
            logger.LogErrorEx(
                $"Can't update server because param is not EXGWrappedServer object:({server.GetType().FullName})");
            return;
        }

        if (currentServerStatusMap.TryGetValue(exgWrappedServer.Info.EndPointDescription, out var exgServerStatus))
        {
            exgWrappedServer.Map = exgServerStatus.Status.Map ?? "<Unknown Map>";
            exgWrappedServer.State = string.Empty;
            exgWrappedServer.CurrentPlayerCount = exgServerStatus.Status.CurrentPlayers;
            exgWrappedServer.MaxPlayerCount = exgServerStatus.Status.MaxPlayers;
        }
    }

    private async void Initialize()
    {
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif
    }

    private Task CheckOrWaitDataReady()
    {
        if (cancellationTokenSource != null)
            return dataReadyTask;

        var dataReadyTaskSource = new TaskCompletionSource();
        cancellationTokenSource = new CancellationTokenSource();
        dataReadyTask = dataReadyTaskSource.Task;
        Task.Run(() => OnWebsocketThread(dataReadyTaskSource, cancellationTokenSource.Token),
            cancellationTokenSource.Token);
        return dataReadyTask;
    }

    private async void OnWebsocketThread(TaskCompletionSource taskCompletionSource,
        CancellationToken cancellationToken)
    {
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif
        logger.LogInformationEx("thread start.");

        var timeInterval = TimeSpan.FromSeconds(10);

        while (!cancellationToken.IsCancellationRequested)
            try
            {
                var resp = await httpFactory.SendAsync(
                    "https://list.darkrp.cn:9000/ServerList/CurrentStatus", default, cancellationToken);

                var str = await resp.Content.ReadAsStreamAsync(cancellationToken);
                var list = new List<EXGServerStatus>();
                await foreach (var status in JsonSerializer.DeserializeAsyncEnumerable<EXGServerStatus>(str,
                                   JsonSerializerOptions.Default, cancellationToken))
                    list.Add(status);
                await UpdateServerStatus(list);

                if (!taskCompletionSource.Task.IsCompleted)
                    taskCompletionSource.SetResult();

                await Task.Delay(timeInterval, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e, "deserialize/update event-stream data failed.");
            }

        logger.LogInformationEx("event-stream thread end.");
    }

    private Task UpdateServerStatus(List<EXGServerStatus> serverStatusList)
    {
        currentServerStatusList = serverStatusList;
        currentServerStatusMap = serverStatusList.ToDictionary(x => $"{x.Server.Ip}:{x.Server.Port}", x => x);
        return Task.CompletedTask;
    }
}