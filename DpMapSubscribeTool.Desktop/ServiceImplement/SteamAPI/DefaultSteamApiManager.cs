using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DpMapSubscribeTool.Services.SteamAPI;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.Injections;
using DpMapSubscribeTool.Utils.MethodExtensions;
using Microsoft.Extensions.Logging;
using Steamworks;

namespace DpMapSubscribeTool.Desktop.ServiceImplement.SteamAPI;

[RegisterInjectable(typeof(ISteamAPIManager))]
public partial class DefaultSteamApiManager : ObservableObject, ISteamAPIManager, IDisposable
{
    private static readonly Regex lineRegex = new(@"\d{2}/\d{2}\s*\d{2}:\d{2}:\d{2}\s*\[(\w+?)\]");

    private static readonly Regex netSteamConnRegex = new(@"handle\s+#\d+\s+\((\d+)\s+(\w+?)\)");
    private readonly AppId_t appId;
    private readonly ILogger<DefaultSteamApiManager> logger;

    [ObservableProperty]
    private bool isEnable;

    private bool isLogParserRunning;

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

    public async Task<bool> CheckIfCS2LogParsingEnable()
    {
        if (Process.GetProcessesByName("cs2").FirstOrDefault() is not Process cs2Proc)
            return false;

        var cmdLine = GetCommandLine(cs2Proc);

        if (!cmdLine.Contains("-condebug"))
            return false;

        if (!MakeSureCS2LogParserIsRunning(cs2Proc))
            return false;

        return true;
    }

    public event Action<NetworkDisconnectionReason> OnJoinServerFailed;

    private static string GetCommandLine(Process process)
    {
        using var searcher =
            new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id);
        using var objects = searcher.Get();
        return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
    }

    private bool MakeSureCS2LogParserIsRunning(Process cs2Proc)
    {
        if (isLogParserRunning)
            return true;

        var exePath = cs2Proc.MainModule.FileName;
        var relativeGamePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(exePath), "..", ".."));
        var csgoPath = Path.Combine(relativeGamePath, "csgo");

        if (!Directory.Exists(csgoPath))
        {
            logger.LogWarningEx($"Can't locate csgo folder path:{csgoPath}");
            return false;
        }

        var consoleLogPath = Path.Combine(csgoPath, "console.log");

        Task.Run(async () =>
        {
            await using var fs =
                File.Open(
                    consoleLogPath,
                    FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
            var reader = new StreamReader(fs);
            var buffer = new char[102400];
            var sb = new StringBuilder();
            fs.Seek(0, SeekOrigin.End);

            while (true)
            {
                if (fs.Position > fs.Length)
                {
                    logger.LogDebugEx("console.log file has been re-created.");
                    fs.Seek(0, SeekOrigin.Begin);
                }

                var read = await reader.ReadAsync(buffer);
                var prevIndex = 0;
                for (var i = 0; i < read; i++)
                {
                    var ch = buffer[i];
                    if (ch == '\n')
                    {
                        sb.Append(buffer[prevIndex.. i]);
                        ProcessConsoleLogLine(sb.ToString());
                        prevIndex = i + 1;
                        sb.Clear();
                    }
                }
            }
        }).NoWait();

        isLogParserRunning = true;
        return true;
    }

    private void ProcessConsoleLogLine(string line)
    {
        var match = lineRegex.Match(line);

        if (!match.Success)
            return;

        var category = match.Groups[1].Value;
        var content = line.Substring(match.Index + match.Length);

        //logger.LogDebugEx($"line: [{category}] {content}");

        switch (category)
        {
            case "NetSteamConn":
                ProcessConsoleLogLine_NetSteamConn(content);
                break;
        }
    }

    private void ProcessConsoleLogLine_NetSteamConn(string content)
    {
        var match = netSteamConnRegex.Match(content);

        if (!match.Success)
            return;

        var codeValue = int.Parse(match.Groups[1].Value);
        var codeName = match.Groups[2].Value;

        var reason = (NetworkDisconnectionReason) (codeValue % 1000);
        logger.LogDebugEx($"handle: ({codeValue}) {reason}");
        OnJoinServerFailed?.Invoke(reason);
    }

    private async void Initialize()
    {
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif
        for (var i = 0; i < 10; i++)
        {
            IsEnable = Steamworks.SteamAPI.Init();
            if (IsEnable)
                break;
            await Task.Delay(100);
        }

        var isRunning = Steamworks.SteamAPI.IsSteamRunning();
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

        new Task(OnSteamAPICallbackRunner, TaskCreationOptions.LongRunning).Start();
        CheckIfCS2LogParsingEnable().NoWait(); //check once
    }

    private async void OnSteamAPICallbackRunner()
    {
        logger.LogInformationEx("thread started.");
        while (true)
        {
            await Task.Yield();
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
        //Steamworks.SteamAPI.Shutdown();
    }
}