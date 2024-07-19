using System;
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
using DpMapSubscribeTool.Services.Settings;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace DpMapSubscribeTool.Desktop.ServiceImplement.Notifications;

[RegisterInjectable(typeof(IApplicationNotification), ServiceLifetime.Singleton)]
public class DefaultApplicationNotification : IApplicationNotification
{
    private readonly Dictionary<string, Action> cachedCallbackMap = new();
    private readonly Dictionary<Notification, string> currentNotications = new();
    private readonly ILogger<DefaultApplicationNotification> logger;
    private readonly IMapManager mapManager;
    private readonly INotificationManager notificationManager;

    private readonly ISettingManager settingManager;
    private ApplicationSettings applicationSettings;
    private AudioFileReader audioFile;
    private string currentLoadedSoundFilePath;
    private bool isDismissOthers;
    private WaveOutEvent outputDevice;

    public DefaultApplicationNotification(ISettingManager settingManager,
        ILogger<DefaultApplicationNotification> logger, IMapManager mapManager)
    {
        this.settingManager = settingManager;
        this.logger = logger;
        this.mapManager = mapManager;

        notificationManager = Program.NotificationManager;
        notificationManager.NotificationActivated += OnNotificationActivated;
        notificationManager.NotificationDismissed += OnNotificationDismissed;
        Initialize();
    }

    public void NofityServerForSubscribeMap(Server server, MapSubscribe subscribe, Action userComfirmCallback)
    {
        if (!applicationSettings.EnableNotication)
            return;
        var isGaming = CheckIfGaming();
        if (isGaming && !applicationSettings.EnableNoticationIfGameForeground)
        {
            logger.LogInformation("raise notification but user is gaming, ignored.");
            return;
        }

        if (applicationSettings.EnableNoticationBySound)
            PlaySound();
        if (applicationSettings.EnableNoticationByTaskbar)
            NotifyTaskBar(server, subscribe, userComfirmCallback);
    }

    private void OnNotificationDismissed(object sender, NotificationDismissedEventArgs e)
    {
        if (sender is Notification notification)
            if (currentNotications.TryGetValue(notification, out var actionId))
                logger.LogInformation($"actionId {actionId} notification has been dismiss by reason:{e.Reason}");

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

    private void OnNotificationActivated(object sender, NotificationActivatedEventArgs e)
    {
        var actionId = e.ActionId;
        if (cachedCallbackMap.TryGetValue(actionId, out var callback))
        {
            logger.LogWarning($"user clicked actionId: {actionId}");
            cachedCallbackMap.Remove(actionId);
            callback?.Invoke();
        }
        else
        {
            logger.LogWarning($"invalided actionId: {actionId}");
        }

        if (isDismissOthers)
            return;
        isDismissOthers = true;
        //dismiss other notifications.
        foreach (var unusedNotificationPair in currentNotications.Where(unusedNotificationPair =>
                     unusedNotificationPair.Value != actionId))
            notificationManager.HideNotification(unusedNotificationPair.Key);
        //clear all
        currentNotications.Clear();
        cachedCallbackMap.Clear();
        isDismissOthers = false;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    private async void Initialize()
    {
        applicationSettings = await settingManager.GetSetting<ApplicationSettings>();
    }

    private void NotifyTaskBar(Server server, MapSubscribe subscribe, Action userComfirmCallback)
    {
        var actionId = RandomHepler.RandomString();
        cachedCallbackMap[actionId] = userComfirmCallback;

        var notification = new Notification();
        notification.Title = "出现订阅地图！";

        var mapTranslationName = mapManager.GetMapTranslationName(server.Map);
        mapTranslationName = !string.IsNullOrWhiteSpace(mapTranslationName) ? $"({mapTranslationName})" : string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"社区: {server.Info.ServerGroupDisplay}  服务器名: {server.Info.Name}");
        sb.AppendLine($"地图: {server.Map} {mapTranslationName}");
        sb.AppendLine($"当前人数: {server.CurrentPlayerCount}/{server.MaxPlayerCount}");
        notification.Body = sb.ToString();

        notification.Buttons.Add(new ValueTuple<string, string>("加入服务器！", actionId));
        notificationManager.ShowNotification(notification, DateTimeOffset.Now + TimeSpan.FromSeconds(15));
        currentNotications[notification] = actionId;
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
        if (audioFile != null)
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
                logger.LogInformation(
                    $"file {applicationSettings.NoticationSoundFilePath} is not found,extract resource notication.mp3 to {filePath} and use.");
            }

            audioFile = new AudioFileReader(filePath);
            outputDevice = new WaveOutEvent();
            outputDevice.Init(audioFile);

            currentLoadedSoundFilePath = filePath;
            logger.LogInformation($"loaded sound file for notification: {currentLoadedSoundFilePath}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Can't prepare sound for notification: {ex.Message}");
            currentLoadedSoundFilePath = default;
        }
    }

    ~DefaultApplicationNotification()
    {
        //ToastNotificationManagerCompat.Uninstall();
    }

    private record JoinActionInfo(string Host, int Port);
}