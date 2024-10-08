﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Dialog;
using DpMapSubscribeTool.Services.Map;
using DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.Test.Bases;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Services.Servers.DefaultImpl.ServiceGroupImpl.Test;

[RegisterInjectable(typeof(IServerActionExecutor), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerInfoSearcher), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(IServerStateUpdater), ServiceLifetime.Singleton)]
[RegisterInjectable(typeof(ITestServerManager), ServiceLifetime.Singleton)]
public partial class TestServerManager : ObservableObject, IServerInfoSearcher, IServerStateUpdater,
    IServerActionExecutor, ITestServerManager
{
    private static readonly ServerInfo info = new()
    {
        Name = "蛆の服",
        ServerGroupDisplay = "测试",
        Host = "127.0.0.1",
        Port = 15000,
        ServerGroup = "Test"
    };

    private readonly IDialogManager dialogManager;

    private readonly ILogger<TestServerManager> logger;
    private readonly IMapManager mapManager;

    private readonly Server server = new TestServer
    {
        Info = info,
        CurrentPlayerCount = 20,
        Delay = 0,
        Map = "ze_ffmako_v5",
        MaxPlayerCount = 64,
        State = "1/1"
    };

    [ObservableProperty]
    private Server serverWaitForUpdate = new TestServer
    {
        Info = info,
        CurrentPlayerCount = 20,
        Delay = 0,
        Map = "ze_ffmako_v5",
        MaxPlayerCount = 64,
        State = "1/1"
    };

    public TestServerManager(ILogger<TestServerManager> logger, IDialogManager dialogManager, IMapManager mapManager)
    {
        this.logger = logger;
        this.dialogManager = dialogManager;
        this.mapManager = mapManager;
    }

    public async Task Join(ServerInfo serverInfo)
    {
        await dialogManager.ShowMessageDialog($"call TestServerManager::Join() serverInfo={serverInfo}");
    }

    public string ServerGroup => "Test";
    public string ServerGroupDescription => "测试";

    public Task<IEnumerable<ServerInfo>> GetAllAvaliableServerInfo()
    {
#if DEBUG
        return Task.FromResult<IEnumerable<ServerInfo>>(new[] {server.Info});
#else
        return Task.FromResult<IEnumerable<ServerInfo>>(Array.Empty<ServerInfo>());
#endif
    }

    public Task<Server> QueryServer(ServerInfo info)
    {
        return Task.FromResult(server);
    }

    public Task UpdateServer(Server server)
    {
        server.Map = ServerWaitForUpdate.Map;
        server.MapTranslationName = mapManager.GetMapTranslationName(ServerGroup, server.Map);
        server.CurrentPlayerCount = ServerWaitForUpdate.CurrentPlayerCount;
        server.MaxPlayerCount = ServerWaitForUpdate.MaxPlayerCount;
        server.State = ServerWaitForUpdate.State;

        return Task.CompletedTask;
    }
}