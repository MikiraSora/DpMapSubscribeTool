using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DpMapSubscribeTool.Services.MessageBox;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Desktop.ServiceImplement.Persistences;

[RegisterInjectable(typeof(IPersistence), ServiceLifetime.Singleton)]
public class DesktopPersistence : IPersistence
{
    private readonly object locker = new();
    private readonly ILogger<DesktopPersistence> logger;
    private readonly IApplicationMessageBox messageBox;
    private readonly IServiceProvider provider;
    private readonly string savePath;

    private readonly JsonSerializerOptions serializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
        WriteIndented = true
    };

    private Dictionary<string, object> settingMap;

    public DesktopPersistence(IServiceProvider provider, ILogger<DesktopPersistence> logger,
        IApplicationMessageBox messageBox)
    {
        this.provider = provider;
        this.logger = logger;
        this.messageBox = messageBox;
        savePath = Path.Combine(Path.GetDirectoryName(typeof(DesktopPersistence).Assembly.Location) ?? string.Empty,
            "setting.json");
    }

    public async Task Save<T>(T obj)
    {
        var key = GetKey<T>();

        settingMap[key] = obj;
        var content = JsonSerializer.Serialize(settingMap, serializerOptions);
        await Task.Run(() =>
        {
            lock (locker)
            {
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
                    settingMap = new Dictionary<string, object>();
                else
                    try
                    {
                        settingMap = JsonSerializer.Deserialize<Dictionary<string, object>>(content, serializerOptions);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Can't load setting.json : {e.Message}");
                        //todo.
                        await messageBox.ShowModalDialog($"无法加载应用配置文件setting.json:{e.Message}",
                            DialogMessageType.Error);
                        Environment.Exit(-1);
                    }
            }
            else
            {
                settingMap = new Dictionary<string, object>();
            }
        }

        if (settingMap.TryGetValue(key, out var jsonContent))
        {
            var node = (JsonElement) jsonContent;
            return node.Deserialize<T>();
        }

        return ActivatorUtilities.CreateInstance<T>(provider);
    }

    private string GetKey<T>()
    {
        return typeof(T).FullName;
    }
}