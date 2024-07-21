using System;
using System.Threading.Tasks;
using DpMapSubscribeTool.Services.SteamAPI;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.Logging;
using Steamworks;

namespace DpMapSubscribeTool.Desktop.ServiceImplement.SteamAPI;

[RegisterInjectable(typeof(ISteamAPIManager))]
public class DefaultSteamApiManager : ISteamAPIManager, IDisposable
{
    private readonly ILogger<DefaultSteamApiManager> logger;

    public DefaultSteamApiManager(ILogger<DefaultSteamApiManager> logger)
    {
        this.logger = logger;
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
    }
}