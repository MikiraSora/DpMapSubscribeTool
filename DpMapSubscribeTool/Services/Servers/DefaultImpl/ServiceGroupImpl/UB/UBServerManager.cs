using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Dialog;
using DpMapSubscribeTool.Services.Map;
using DpMapSubscribeTool.Services.Networks;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.UB.Bases;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.Injections;
using DpMapSubscribeTool.Utils.MethodExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.UB;

[RegisterInjectable(typeof(IServerActionExecutor), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerInfoSearcher), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerSqueezeJoinRunner), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerStateUpdater), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IUBServerServiceBase), ServiceLifetime.Singleton)]
public class UBServerManager : IUBServerServiceBase, IServerInfoSearcher, IServerStateUpdater,
    IServerSqueezeJoinRunner,
    IServerActionExecutor
{
    private readonly CommonJoinServer commonJoinServer;
    private readonly IDialogManager dialogManager;
    private readonly IApplicationHttpFactory httpFactory;
    private readonly ILogger<UBServerManager> logger;
    private readonly IMapManager mapManager;

    private readonly JsonSerializerOptions MsgDeserializeOption = new()
    {
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement
    };

    private readonly IDictionary<string, Func<UBEventMsg, CancellationToken, Task>> msgProcessorMap;

    private readonly IPersistence persistence;
    private readonly IServiceProvider provider;

    private CancellationTokenSource cancellationTokenSource;

    private Task dataReadyTask;

    private UBServerSettings ubServerSettings;

    public UBServerManager(IApplicationHttpFactory httpFactory, ILogger<UBServerManager> logger,
        IServiceProvider provider, CommonJoinServer commonJoinServer,
        IMapManager mapManager, IDialogManager dialogManager, IPersistence persistence)
    {
        this.httpFactory = httpFactory;
        this.logger = logger;
        this.provider = provider;
        this.commonJoinServer = commonJoinServer;
        this.mapManager = mapManager;
        this.dialogManager = dialogManager;
        this.persistence = persistence;

        msgProcessorMap = new Dictionary<string, Func<UBEventMsg, CancellationToken, Task>>
        {
            {"server/init", ProcessMsg_ServerInit},
            {"server/client/connected", ProcessMsg_ServerClientConnected},
            {"server/client/disconnect", ProcessMsg_ServerClientDisConnected},
            {"server/nextmap/load", ProcessMsg_ServerNextmapLoad},
            {"server/nominate/add", ProcessMsg_ServerNominateAdd},
            {"server/round_start", ProcessMsg_ServerRoundStart},
            {"server/levelchange", ProcessMsg_ServerLevelchange},
            {"server/nominate/remove", ProcessMsg_ServerNominateRemove}
        };

        Initialize();
    }

    public Task Join(ServerInfo serverInfo)
    {
        return commonJoinServer.JoinServer(serverInfo.Host, serverInfo.Port);
    }

    public async Task<IEnumerable<ServerInfo>> GetAllAvaliableServerInfo()
    {
        await CheckOrWaitDataReady();

        var regex = new Regex(@"Q群\s\d+");

        return servers.Values.Select(x => new ServerInfo
        {
            ServerGroupDisplay = ServerGroupDescription,
            ServerGroup = ServerGroup,
            //simply names
            Name = regex.Replace(x.Name, string.Empty, 1).Trim(),
            Host = x.Host,
            Port = x.Port
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
        var ubWrappedServer = new UBWrappedServer
        {
            Info = info
        };

        await UpdateServer(ubWrappedServer);

        return ubWrappedServer;
    }

    public async Task UpdateServer(Server server)
    {
        await CheckOrWaitDataReady();

        if (server is not UBWrappedServer ubWrappedServer)
        {
            logger.LogErrorEx(
                $"Can't update server because param is not UBWrappedServer object:({server.GetType().FullName})");
            return;
        }

        if (endPointServerMap.TryGetValue(ubWrappedServer.Info.EndPointDescription, out var ubServer))
        {
            ubWrappedServer.Map = ubServer.CurrentMap?.Name ?? "<Unknown Map>";
            ubWrappedServer.MapTranslationName = mapManager.GetMapTranslationName(ServerGroup, ubWrappedServer.Map);
            ubWrappedServer.State = $"{ubServer.NumRounds}/{ubServer.MaxRounds}";
            ubWrappedServer.CurrentPlayerCount = ubServer.Clients.Count;
            ubWrappedServer.MaxPlayerCount = ubServer.MaxPlayers;
        }
    }

    public string ServerGroup => "UB";
    public string ServerGroupDescription => "MoeUB";

    private async void Initialize()
    {
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif
        ubServerSettings = await persistence.Load<UBServerSettings>();
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

        var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri("wss://ws2.moeub.cn/ws?files=0&appid=730"), default);
        logger.LogInformationEx("websocket connected successfully.");
        var recvBuffer = new byte[1024];
        var recvStream = new MemoryStream();
        var read = 0;

        if (!taskCompletionSource.Task.IsCompleted)
            Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None)
                .ContinueWith(t => taskCompletionSource.SetResult(), CancellationToken.None)
                .NoWait();

        while (!cancellationToken.IsCancellationRequested)
            try
            {
                var result = await client.ReceiveAsync(recvBuffer, cancellationToken);
                await recvStream.WriteAsync(recvBuffer, 0, result.Count, cancellationToken);
                read += result.Count;

                if (result.EndOfMessage)
                {
                    //process
                    var rawMsg = Encoding.UTF8.GetString(recvStream.GetBuffer(), 0, read);
                    await ProcessMsg(rawMsg, cancellationToken);

                    //clean recv state
                    recvStream.Seek(0, SeekOrigin.Begin);
                    read = 0;
                }
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e, "wss recv message failed.");
            }

        logger.LogInformationEx("thread end.");
    }


    private async Task ProcessMsg(string rawMsg,
        CancellationToken cancellationToken)
    {
        var msg = JsonSerializer.Deserialize<UBEventMsg>(rawMsg, MsgDeserializeOption);

        if (msgProcessorMap.TryGetValue(msg.Event, out var processorFunc))
            await processorFunc(msg, cancellationToken);
    }

    #region Msg Processors

    private readonly Dictionary<int, UBServer> servers = new();
    private readonly Dictionary<string, UBServer> endPointServerMap = new();

    private Task ProcessMsg_ServerInit(UBEventMsg ubEventMsg, CancellationToken cancellationToken)
    {
        //register server info
        var server = ubEventMsg.GetDataAs<UBServer>();

        servers[server.Id] = server;
        endPointServerMap[$"{server.Host}:{server.Port}"] = server;
        logger.LogInformationEx($"init server id:{server.Id}, name:{server.Name}");
        mapManager.CacheMapTranslationName(ServerGroup, server.CurrentMap.Name, server.CurrentMap.Label);

        return Task.CompletedTask;
    }

    private Task ProcessMsg_ServerClientConnected(UBEventMsg ubEventMsg, CancellationToken cancellationToken)
    {
        if (!servers.TryGetValue(ubEventMsg.ServerId, out var server))
        {
            logger.LogWarningEx($"server id:{ubEventMsg.ServerId} not found");
            return Task.CompletedTask;
        }

        var client = ubEventMsg.GetDataAs<UBClient>();
        server.Clients.Add(client);

        logger.LogDebugEx(
            $"user {client.Name}(steamId:{client.Steam64}) join server:{server.Id}, name:{server.Name}");
        return Task.CompletedTask;
    }

    private Task ProcessMsg_ServerClientDisConnected(UBEventMsg ubEventMsg, CancellationToken cancellationToken)
    {
        if (!servers.TryGetValue(ubEventMsg.ServerId, out var server))
        {
            logger.LogWarningEx($"server id:{ubEventMsg.ServerId} not found");
            return Task.CompletedTask;
        }

        var client = ubEventMsg.GetDataAs<UBClient>();
        if (server.Clients.FirstOrDefault(x => x.Index == client.Index) is not UBClient actualClient)
        {
            logger.LogWarningEx(
                $"user index {client.Index} is not found in server:{server.Id}, name:{server.Name}");
            return Task.CompletedTask;
        }

        server.Clients.Remove(actualClient);
        logger.LogDebugEx(
            $"user {client.Name}(steamId:{client.Steam64}) leave server:{server.Id}, name:{server.Name}");
        return Task.CompletedTask;
    }

    private Task ProcessMsg_ServerNextmapLoad(UBEventMsg ubEventMsg, CancellationToken cancellationToken)
    {
        if (!servers.TryGetValue(ubEventMsg.ServerId, out var server))
        {
            logger.LogWarningEx($"server id:{ubEventMsg.ServerId} not found");
            return Task.CompletedTask;
        }

        var map = ubEventMsg.GetDataAs<UBMap>();
        server.CurrentMap = map;
        server.NextMap = default;

        logger.LogDebugEx(
            $"server:{server.Id}, name:{server.Name} has loaded nextmap:{map.Name}");
        return Task.CompletedTask;
    }

    private Task ProcessMsg_ServerNominateRemove(UBEventMsg ubEventMsg, CancellationToken cancellationToken)
    {
        if (!servers.TryGetValue(ubEventMsg.ServerId, out var server))
        {
            logger.LogWarningEx($"server id:{ubEventMsg.ServerId} not found");
            return Task.CompletedTask;
        }

        var nominate = ubEventMsg.GetDataAs<UBNominate>();
        if (server.Nominate.FirstOrDefault(x => x.Name == nominate.Name) is not UBNominate actualNominate)
        {
            logger.LogWarningEx(
                $"map nomination {nominate.Name} is not found in server:{server.Id}, name:{server.Name}");
            return Task.CompletedTask;
        }

        server.Nominate.Remove(actualNominate);

        logger.LogDebugEx(
            $"server:{server.Id}, name:{server.Name} has removed map nomination: {nominate.Name}");
        return Task.CompletedTask;
    }

    private Task ProcessMsg_ServerLevelchange(UBEventMsg ubEventMsg, CancellationToken cancellationToken)
    {
        if (!servers.TryGetValue(ubEventMsg.ServerId, out var server))
        {
            logger.LogWarningEx($"server id:{ubEventMsg.ServerId} not found");
            return Task.CompletedTask;
        }

        var level = ubEventMsg.GetDataAs<UBLevel>();
        server.Level = level;

        logger.LogDebugEx(
            $"server:{server.Id}, name:{server.Name} has changed level:{level.Name} {level.Num}/{level.Max}");
        return Task.CompletedTask;
    }


    private Task ProcessMsg_ServerRoundStart(UBEventMsg ubEventMsg, CancellationToken cancellationToken)
    {
        if (!servers.TryGetValue(ubEventMsg.ServerId, out var server))
        {
            logger.LogWarningEx($"server id:{ubEventMsg.ServerId} not found");
            return Task.CompletedTask;
        }

        //{"maxrounds":0,"numrounds":0}
        var inputServer = ubEventMsg.GetDataAs<UBServer>();
        server.MaxRounds = inputServer.MaxRounds;
        server.NumRounds = inputServer.NumRounds;

        logger.LogDebugEx(
            $"server:{server.Id}, name:{server.Name} round started: {server.NumRounds}/{server.MaxRounds}");
        return Task.CompletedTask;
    }

    private Task ProcessMsg_ServerNominateAdd(UBEventMsg ubEventMsg, CancellationToken cancellationToken)
    {
        if (!servers.TryGetValue(ubEventMsg.ServerId, out var server))
        {
            logger.LogWarningEx($"server id:{ubEventMsg.ServerId} not found");
            return Task.CompletedTask;
        }

        var nominate = ubEventMsg.GetDataAs<UBNominate>();
        server.Nominate.Add(nominate);

        logger.LogDebugEx(
            $"server:{server.Id}, name:{server.Name} has added map nomination: {nominate.Name}");
        return Task.CompletedTask;
    }

    private Task ProcessMsg_ServerclientSpawn(UBEventMsg ubEventMsg, CancellationToken cancellationToken)
    {
        if (!servers.TryGetValue(ubEventMsg.ServerId, out var server))
        {
            logger.LogWarningEx($"server id:{ubEventMsg.ServerId} not found");
            return Task.CompletedTask;
        }

        var alive = ubEventMsg.GetDataAs<UBClient>().Alive;
        if (server.Clients.FirstOrDefault(x => x.Index == ubEventMsg.ClientId) is not UBClient actualClient)
        {
            logger.LogWarningEx(
                $"user index {ubEventMsg.ClientId} is not found in server:{server.Id}, name:{server.Name}");
            return Task.CompletedTask;
        }

        actualClient.Alive = alive;

        logger.LogDebugEx(
            $"in server:{server.Id}, name:{server.Name}, user {actualClient.Name}(steamId:{actualClient.Steam64}) Alive = {alive}");
        return Task.CompletedTask;
    }

    #endregion
}