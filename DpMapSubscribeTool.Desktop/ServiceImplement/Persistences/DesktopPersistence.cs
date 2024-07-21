using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using DpMapSubscribeTool.Services.Dialog;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Desktop.ServiceImplement.Persistences;

[RegisterInjectable(typeof(IPersistence), ServiceLifetime.Singleton)]
public class DesktopPersistence : IPersistence
{
    private readonly Dictionary<string, object> cacheObj = new();
    private readonly IDialogManager dialogManager;
    private readonly object locker = new();
    private readonly ILogger<DesktopPersistence> logger;
    private readonly IServiceProvider provider;
    private readonly string savePath;

    private readonly JsonSerializerOptions serializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    private Dictionary<string, string> settingMap;

    public DesktopPersistence(IServiceProvider provider, ILogger<DesktopPersistence> logger,
        IDialogManager dialogManager)
    {
        this.provider = provider;
        this.logger = logger;
        this.dialogManager = dialogManager;
        savePath = Path.Combine(Path.GetDirectoryName(typeof(DesktopPersistence).Assembly.Location) ?? string.Empty,
            "setting.json");
    }

    public async Task Save<T>(T obj)
    {
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return;
#endif

        await Task.Run(() =>
        {
            lock (locker)
            {
                var key = GetKey<T>();

                settingMap[key] = JsonSerializer.Serialize(obj, serializerOptions);
                var content = JsonSerializer.Serialize(settingMap, serializerOptions);

                File.WriteAllText(savePath, content);
            }
        });
    }

    public Task<T> Load<T>()
    {
        return Task.Run(() =>
        {
            lock (locker)
            {
                return LoadInternal<T>();
            }
        });
    }

    private T LoadInternal<T>()
    {
        var key = GetKey<T>();

        if (cacheObj.TryGetValue(key, out var obj))
        {
            logger.LogDebugEx($"return cached {typeof(T).Name} object, hash = {obj.GetHashCode()}");
            return (T) obj;
        }

        if (settingMap is null)
        {
            if (File.Exists(savePath))
            {
                var content = File.ReadAllText(savePath);
                if (string.IsNullOrWhiteSpace(content))
                    settingMap = new Dictionary<string, string>();
                else
                    try
                    {
                        settingMap = JsonSerializer.Deserialize<Dictionary<string, string>>(content, serializerOptions);
                    }
                    catch (Exception e)
                    {
                        logger.LogErrorEx(e, $"Can't load setting.json : {e.Message}");
                        Task.Run(async () =>
                        {
                            await dialogManager.ShowMessageDialog($"无法加载应用配置文件setting.json:{e.Message}",
                                DialogMessageType.Error);
                            Environment.Exit(-1);
                        }).Wait();
                    }
            }
            else
            {
                settingMap = new Dictionary<string, string>();
            }
        }

        T cw = default;
        if (settingMap.TryGetValue(key, out var jsonContent))
        {
            cw = JsonSerializer.Deserialize<T>(jsonContent);
            logger.LogDebugEx($"create new {typeof(T).Name} object from setting.json, hash = {cw.GetHashCode()}");
        }
        else
        {
            cw = ActivatorUtilities.CreateInstance<T>(provider);
            logger.LogDebugEx(
                $"create new {typeof(T).Name} object from ActivatorUtilities.CreateInstance(), hash = {cw.GetHashCode()}");
        }

        cacheObj[key] = cw;
        return cw;
    }

    private string GetKey<T>()
    {
        return typeof(T).FullName;
    }
}