﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DesktopNotifications;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Map;
using DpMapSubscribeTool.Services.Notifications;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace DpMapSubscribeTool.Desktop.ServiceImplement.Notifications;

[RegisterInjectable(typeof(IApplicationNotification), ServiceLifetime.Singleton)]
public class DefaultApplicationNotification : IApplicationNotification
{
    private readonly Dictionary<string, Action<UserReaction>> cachedCallbackMap = new();
    private readonly Dictionary<Notification, string> currentNotications = new();
    private readonly ILogger<DefaultApplicationNotification> logger;
    private readonly IMapManager mapManager;
    private readonly INotificationManager notificationManager;
    private readonly IPersistence persistence;

    private ApplicationSettings applicationSettings;
    private AudioFileReader audioFile;
    private string currentLoadedSoundFilePath;
    private bool isDismissOthers;
    private WaveOutEvent outputDevice;

    public DefaultApplicationNotification(IPersistence persistence,
        ILogger<DefaultApplicationNotification> logger, IMapManager mapManager)
    {
        this.persistence = persistence;
        this.logger = logger;
        this.mapManager = mapManager;

        notificationManager = Program.NotificationManager;
        notificationManager.NotificationActivated += OnNotificationActivated;
        notificationManager.NotificationDismissed += OnNotificationDismissed;
        Initialize();
    }

    public void NofityServerForSubscribeMap(Server server, MapSubscribe subscribe,
        Action<UserReaction> userComfirmCallback)
    {
        if (!applicationSettings.EnableNotication)
            return;
        var isGaming = CheckIfGaming();
        if (isGaming && !applicationSettings.EnableNoticationIfGameForeground)
        {
            logger.LogInformationEx("raise notification but user is gaming, ignored.");
            return;
        }

        if (applicationSettings.EnableNoticationBySound)
            PlaySound();
        if (applicationSettings.EnableNoticationByTaskbar)
            NotifySqueezeJoinServerSuccessTaskBar(server, subscribe, userComfirmCallback);
    }

    public void NofitySqueezeJoinSuccess(ServerInfo serverInfo, Action<UserReaction> userComfirmCallback)
    {
        if (!applicationSettings.EnableNotication)
            return;
        var isGaming = CheckIfGaming();
        if (isGaming && !applicationSettings.EnableNoticationIfGameForeground)
        {
            logger.LogInformationEx("raise notification but user is gaming, ignored.");
            return;
        }

        if (applicationSettings.EnableNoticationBySound)
            PlaySound();
        if (applicationSettings.EnableNoticationByTaskbar)
            NotifySqueezeJoinServerSuccessTaskBar(serverInfo, userComfirmCallback);
    }

    private void DismissAllAndClear()
    {
        if (isDismissOthers)
            return;
        isDismissOthers = true;
        //dismiss other notifications.
        foreach (var unusedNotification in currentNotications.Keys)
            notificationManager.HideNotification(unusedNotification);
        //clear all
        currentNotications.Clear();
        cachedCallbackMap.Clear();
        isDismissOthers = false;
    }

    private void OnNotificationDismissed(object sender, NotificationDismissedEventArgs e)
    {
        var notification = e.Notification;
        if (currentNotications.TryGetValue(notification, out var actionId))
        {
            logger.LogInformationEx($"actionId {actionId} notification has been dismiss by reason:{e.Reason}");
            if (cachedCallbackMap.TryGetValue(actionId, out var callback))
                callback(UserReaction.Dismiss);
            else if ("default" != actionId)
                logger.LogWarningEx($"invalided actionId: {actionId}");
            cachedCallbackMap.Remove(actionId);
        }

        currentNotications.Remove(notification);

        DismissAllAndClear();
    }

    private void OnNotificationActivated(object sender, NotificationActivatedEventArgs e)
    {
        if (sender is Notification notification)
            currentNotications.Remove(notification);

        var actionId = e.ActionId;
        if (cachedCallbackMap.TryGetValue(actionId, out var callback))
        {
            logger.LogInformationEx($"user clicked actionId: {actionId}");
            cachedCallbackMap.Remove(actionId);
            callback(UserReaction.Dismiss);
        }
        else if ("default" != actionId)
        {
            logger.LogWarningEx($"invalided actionId: {actionId}");
        }

        DismissAllAndClear();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    private async void Initialize()
    {
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif
        applicationSettings = await persistence.Load<ApplicationSettings>();
    }

    private void CreateCommonNotifyTaskBar(string title, string content, string buttonText,
        Action<UserReaction> buttonCallback)
    {
        var actionId = RandomHepler.RandomString();
        cachedCallbackMap[actionId] = buttonCallback;

        var notification = new Notification();
        notification.Title = title;

        notification.Body = content;

        notification.Buttons.Add(new ValueTuple<string, string>(buttonText, actionId));

        notificationManager.ShowNotification(notification, DateTimeOffset.Now + TimeSpan.FromSeconds(15));
        currentNotications[notification] = actionId;
        logger.LogInformationEx($"show new notification, actionId: {actionId}");
    }

    private void NotifySqueezeJoinServerSuccessTaskBar(ServerInfo serverInfo, Action<UserReaction> userComfirmCallback)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"社区: {serverInfo.ServerGroupDisplay}  服务器名: {serverInfo.Name}");
        sb.AppendLine("挤服功能已检测到服务器人数空闲,已执行进入服务器命令,请切回游戏检查是否成功");

        CreateCommonNotifyTaskBar("挤服成功！", sb.ToString(), "关闭", userComfirmCallback);
    }

    private void NotifySqueezeJoinServerSuccessTaskBar(Server server, MapSubscribe subscribe,
        Action<UserReaction> userComfirmCallback)
    {
        var mapTranslationName = mapManager.GetMapTranslationName(server.Map);
        mapTranslationName = !string.IsNullOrWhiteSpace(mapTranslationName) ? $"({mapTranslationName})" : string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"社区: {server.Info.ServerGroupDisplay}  服务器名: {server.Info.Name}");
        sb.AppendLine($"地图: {server.Map} {mapTranslationName}");
        sb.AppendLine($"当前人数: {server.CurrentPlayerCount}/{server.MaxPlayerCount}");

        CreateCommonNotifyTaskBar("出现订阅地图！", sb.ToString(), "加入服务器！", userComfirmCallback);
    }

    private bool CheckIfGaming()
    {
        var foregroundHwnd = GetForegroundWindow();
        if (foregroundHwnd == IntPtr.Zero)
            return false;

        GetWindowThreadProcessId(foregroundHwnd, out var foregroundPid);

        return check("cs2") || check("csgo");

        bool check(string processName)
        {
            return Process.GetProcessesByName(processName).Any(proc => proc.Id == foregroundPid);
        }
    }

    private void PlaySound()
    {
        PrepareAudio();

        outputDevice?.Stop();
        audioFile?.Seek(0, SeekOrigin.Begin);
        outputDevice?.Play();
    }

    private void PrepareAudio()
    {
        if (audioFile == null)
        {
            //check file changed.
            if (currentLoadedSoundFilePath == applicationSettings.NoticationSoundFilePath)
                return;

            audioFile?.Dispose();
            outputDevice?.Dispose();
        }

        try
        {
            var filePath = applicationSettings.NoticationSoundFilePath;
            if (!File.Exists(filePath))
            {
                filePath = TempFileHelper.GetTempFilePath("sound", "notification", ".mp3", false);
                //unzip default sound to temp path.
                using var rs =
                    typeof(Program).Assembly.GetManifestResourceStream(
                        "DpMapSubscribeTool.Desktop.Resources.notification.mp3");
                using var fs = File.OpenWrite(filePath);
                rs.CopyTo(fs);
                logger.LogInformationEx(
                    $"file {applicationSettings.NoticationSoundFilePath} is not found,extract resource notication.mp3 to {filePath} and use.");
            }

            audioFile = new AudioFileReader(filePath);
            outputDevice = new WaveOutEvent();
            outputDevice.Init(audioFile);

            currentLoadedSoundFilePath = filePath;
            logger.LogInformationEx($"loaded sound file for notification: {currentLoadedSoundFilePath}");
        }
        catch (Exception ex)
        {
            logger.LogErrorEx(ex, $"Can't prepare sound for notification: {ex.Message}");
            currentLoadedSoundFilePath = default;
        }
    }

    ~DefaultApplicationNotification()
    {
        //ToastNotificationManagerCompat.Uninstall();
    }

    private record JoinActionInfo(string Host, int Port);
}