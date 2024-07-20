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

    public async Task<T> Load<T>()
    {
        var key = GetKey<T>();

        if (settingMap is null)
        {
            if (File.Exists(savePath))
            {
                var content = "";
                await Task.Run(() =>
                {
                    lock (locker)
                    {
                        content = File.ReadAllText(savePath);
                    }
                });
                if (string.IsNullOrWhiteSpace(content))
                    settingMap = new Dictionary<string, string>();
                else
                    try
                    {
                        settingMap = JsonSerializer.Deserialize<Dictionary<string, string>>(content, serializerOptions);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Can't load setting.json : {e.Message}");
                        await dialogManager.ShowMessageDialog($"无法加载应用配置文件setting.json:{e.Message}",
                            DialogMessageType.Error);
                        Environment.Exit(-1);
                    }
            }
            else
            {
                settingMap = new Dictionary<string, string>();
            }
        }

        if (settingMap.TryGetValue(key, out var jsonContent))
            return JsonSerializer.Deserialize<T>(jsonContent);

        return ActivatorUtilities.CreateInstance<T>(provider);
    }

    private string GetKey<T>()
    {
        return typeof(T).FullName;
    }
}