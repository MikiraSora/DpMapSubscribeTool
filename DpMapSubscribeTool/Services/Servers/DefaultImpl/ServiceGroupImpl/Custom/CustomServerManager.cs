using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Dialog;
using DpMapSubscribeTool.Services.Map;
using DpMapSubscribeTool.Services.Networks;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Services.SteamAPI;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.Injections;
using DpMapSubscribeTool.Utils.MethodExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.Custom;

[RegisterInjectable(typeof(IServerActionExecutor), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerInfoSearcher), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerSqueezeJoinRunner), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerStateUpdater), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(ICustomServerServiceBase), ServiceLifetime.Singleton)]
public class CustomServerManager : ICustomServerServiceBase, IServerInfoSearcher, IServerStateUpdater,
    IServerSqueezeJoinRunner,
    IServerActionExecutor
{
    private readonly CommonJoinServer commonJoinServer;
    private readonly IDialogManager dialogManager;
    private readonly IApplicationHttpFactory httpFactory;
    private readonly ILogger<CustomServerManager> logger;
    private readonly IMapManager mapManager;
    private readonly IPersistence persistence;
    private readonly ISteamAPIManager steamApiManager;

    private CancellationTokenSource cancellationTokenSource;
    private QuestServerResult[] currentServerStatusList;
    private Dictionary<string, QuestServerResult> currentServerStatusMap;
    private CustomServerSettings customServerSettings;

    private Task dataReadyTask;

    public CustomServerManager(IApplicationHttpFactory httpFactory, ILogger<CustomServerManager> logger,
        CommonJoinServer commonJoinServer, ISteamAPIManager steamApiManager,
        IMapManager mapManager, IDialogManager dialogManager, IPersistence persistence)
    {
        this.httpFactory = httpFactory;
        this.logger = logger;
        this.commonJoinServer = commonJoinServer;
        this.steamApiManager = steamApiManager;
        this.mapManager = mapManager;
        this.dialogManager = dialogManager;
        this.persistence = persistence;

        Initialize();
    }

    public Task Join(ServerInfo serverInfo)
    {
        return commonJoinServer.JoinServer(serverInfo.Host, serverInfo.Port);
    }

    public async Task<IEnumerable<ServerInfo>> GetAllAvaliableServerInfo()
    {
        await CheckOrWaitDataReady();
        return customServerSettings.CustomServerInfos;
    }

    public string ServerGroup => "Custom";

    //unused.
    public string ServerGroupDescription => "自定义";

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
        var exgWrappedServer = new CustomWrappedServer
        {
            Info = info
        };

        await UpdateServer(exgWrappedServer);

        return exgWrappedServer;
    }

    public async Task UpdateServer(Server server)
    {
        await CheckOrWaitDataReady();

        if (server is not CustomWrappedServer customWrappedServer)
        {
            logger.LogErrorEx(
                $"Can't update server because param is not CustomWrappedServer object:({server.GetType().FullName})");
            return;
        }

        if (currentServerStatusMap.TryGetValue(customWrappedServer.Info.EndPointDescription, out var customServer))
        {
            customWrappedServer.Map = customServer.Map ?? "<Unknown Map>";
            customWrappedServer.MapTranslationName = mapManager.GetMapTranslationName(ServerGroup, customWrappedServer.Map);
            customWrappedServer.State = string.Empty;
            customWrappedServer.CurrentPlayerCount = customServer.CurrentPlayerCount;
            customWrappedServer.MaxPlayerCount = customServer.MaxPlayerCount;
            customWrappedServer.Delay = customServer.Delay;
        }
    }

    private async void Initialize()
    {
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif

        customServerSettings = await persistence.Load<CustomServerSettings>();
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
        {
            try
            {
                var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                Task.Delay(TimeSpan.FromSeconds(1), cancellationToken)
                    .ContinueWith(t => cancelSource.Cancel(), cancellationToken).NoWait();

                var tasks = customServerSettings.CustomServerInfos.Select(serverInfo =>
                    steamApiManager.QueryServer(serverInfo.Host, serverInfo.Port, cancelSource.Token)).ToArray();

                var queryResult = await Task.WhenAll(tasks);
                UpdateQueryServers(queryResult.OfType<QuestServerResult>().ToArray());
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e, "update data failed.");
            }

            if (!taskCompletionSource.Task.IsCompleted)
                taskCompletionSource.SetResult();
            await Task.Delay(timeInterval, cancellationToken);
        }

        logger.LogInformationEx("thread end.");
    }

    private void UpdateQueryServers(QuestServerResult[] queryResult)
    {
        currentServerStatusList = queryResult;
        currentServerStatusMap = queryResult.ToDictionary(x => $"{x.Host}:{x.Port}", x => x);
        logger.LogDebugEx("servers updated.");
    }
}