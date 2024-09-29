using System;
using System.Threading.Tasks;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.Injections;
using DpMapSubscribeTool.Utils.MethodExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Services.Map.DefaultImpl;

[RegisterInjectable(typeof(IMapManager), ServiceLifetime.Singleton)]
public class DefaultMapManager : IMapManager
{
    private readonly ILogger<DefaultMapManager> logger;
    private readonly IPersistence persistence;
    private TranslationMapData translationMapData;

    public DefaultMapManager(IPersistence persistence, ILogger<DefaultMapManager> logger)
    {
        this.persistence = persistence;
        this.logger = logger;
        Initialize();
    }

    public string GetMapTranslationName(string serviceGroup, string mapName)
    {
        var serverGruopKey = $"{serviceGroup}:{mapName}";
        var defaultKey = $":{mapName}";

        return translationMapData.TranslationNames.TryGetValue(serverGruopKey, out var mapTranslationName)
            ? mapTranslationName
            : translationMapData.TranslationNames.TryGetValue(defaultKey, out var defaultTranslationName)
                ? defaultTranslationName
                : default;
    }

    public void CacheMapTranslationName(string serviceGroup, string mapName, string mapTranslationName)
    {
        if (string.IsNullOrWhiteSpace(mapTranslationName))
            return;
        var serverGruopKey = $"{serviceGroup}:{mapName}";
        var defaultKey = $":{mapName}";

        translationMapData.TranslationNames[serverGruopKey] = mapTranslationName;
        translationMapData.TranslationNames.TryAdd(defaultKey, mapTranslationName);
    }

    private async void Initialize()
    {
#if DEBUG
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif
        translationMapData = await persistence.Load<TranslationMapData>();
        
        new Task(OnAutoSaveDataTask, TaskCreationOptions.LongRunning).Start();
    }

    private async void OnAutoSaveDataTask()
    {
        while (true)
            try
            {
                await persistence.Save(translationMapData);
                logger.LogDebugEx("TranslationMapData saved.");
                await Task.Delay(TimeSpan.FromSeconds(15));
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e, $"TranslationMapData failed: {e.Message}");
            }
    }
}