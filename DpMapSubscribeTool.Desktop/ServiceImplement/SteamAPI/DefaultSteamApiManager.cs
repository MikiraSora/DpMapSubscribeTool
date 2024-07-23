using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DpMapSubscribeTool.Services.SteamAPI;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.Injections;
using DpMapSubscribeTool.Utils.MethodExtensions;
using Microsoft.Extensions.Logging;
using Steamworks;

namespace DpMapSubscribeTool.Desktop.ServiceImplement.SteamAPI;

[RegisterInjectable(typeof(ISteamAPIManager))]
public class DefaultSteamApiManager : ISteamAPIManager, IDisposable
{
    private readonly AppId_t appId;
    private readonly ILogger<DefaultSteamApiManager> logger;

    public DefaultSteamApiManager(ILogger<DefaultSteamApiManager> logger)
    {
        this.logger = logger;
        appId = new AppId_t(730);
        Initialize();
    }

    public void Dispose()
    {
        Steamworks.SteamAPI.Shutdown();
        IsEnable = false;
        logger.LogDebugEx("SteamAPI shutdown.");
    }

    public bool IsEnable { get; private set; }

    public Task<QueryUserNameResult> GetCurrentLoginUserName()
    {
        if (!IsEnable)
            throw new Exception("DefaultStreamAPIManager dosen't initialize.");

        if (!Steamworks.SteamAPI.IsSteamRunning())
        {
            logger.LogErrorEx("Can't get user name because Steam is not running.");
            return Task.FromResult(new QueryUserNameResult(false, default));
        }

        return Task.FromResult(new QueryUserNameResult(true, SteamFriends.GetPersonaName()));
    }

    public Task<byte[]> GetMapThumbPictureImageData(string mapName)
    {
        throw new NotImplementedException();
    }

    public async Task<QuestServerResult> QueryServer(string host, int port, CancellationToken ct)
    {
        var ipAddrs = await Dns.GetHostAddressesAsync(host);
        if (ipAddrs.Length == 0)
        {
            logger.LogErrorEx($"can't lookup host actual IP address, host:{host}");
            return null;
        }

        var ipAddr = ipAddrs.First();
        var bytes = ipAddr.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        var ipAddrInt = BitConverter.ToUInt32(bytes, 0);

        var taskSource = new TaskCompletionSource<QuestServerResult>();
        var resp = new ISteamMatchmakingPingResponse(server =>
        {
            var result = new QuestServerResult
            {
                Map = server.GetMap(),
                CurrentPlayerCount = server.m_nPlayers,
                MaxPlayerCount = server.m_nMaxPlayers,
                Name = server.GetServerName(),
                Delay = server.m_nPing,
                Host = host,
                Port = port
            };

            if (!ct.IsCancellationRequested)
                taskSource.SetResult(result);
        }, () =>
        {
            logger.LogErrorEx($"call SteamMatchmakingServers.PingServer() failed, ep:{host}:{port}");
            if (!ct.IsCancellationRequested)
                taskSource.SetResult(default);
        });

        var handle = SteamMatchmakingServers.PingServer(ipAddrInt, (ushort) port, resp);

        ct.Register(() =>
        {
            if (taskSource.Task.IsCompleted)
                return;
            taskSource.SetResult(default);
        });
        var queryResult = await taskSource.Task;
        return queryResult;
    }

    private async void Initialize()
    {
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif
        var isRunning = Steamworks.SteamAPI.IsSteamRunning();
        for (var i = 0; i < 10; i++)
        {
            IsEnable = Steamworks.SteamAPI.Init();
            if (IsEnable)
                break;
            await Task.Delay(100);
        }

        logger.LogInformationEx($"SteamAPI initialized, isRunning = {isRunning}, IsEnable = {IsEnable}.");

        if (!Packsize.Test())
        {
            logger.LogWarningEx("Program is using the wrong Steamworks.NET Assembly for this platform!");
            IsEnable = false;
        }

        if (!DllCheck.Test())
        {
            logger.LogWarningEx("Program is using the wrong dlls for this platform!");
            IsEnable = false;
        }

        Task.Run(OnSteamAPICallbackRunner).NoWait();
    }

    private async void OnSteamAPICallbackRunner()
    {
        logger.LogInformationEx("thread started.");
        while (true)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10));
            if (!IsEnable)
            {
                IsEnable = Steamworks.SteamAPI.Init();
                continue;
            }

            try
            {
                Steamworks.SteamAPI.RunCallbacks();
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e, $"call RunCallbacks() failed: {e.Message}");
            }
        }
    }

    ~DefaultSteamApiManager()
    {
        Steamworks.SteamAPI.Shutdown();
    }
}