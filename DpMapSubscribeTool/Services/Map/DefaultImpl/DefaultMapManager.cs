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

    public string GetMapTranslationName(string mapName)
    {
        return translationMapData.TranslationNames.TryGetValue(mapName, out var mapTranslationName)
            ? mapTranslationName
            : default;
    }

    public void CacheMapTranslationName(string mapName, string mapTranslationName, bool enableOverwrite)
    {
        if (translationMapData.TranslationNames.TryGetValue(mapName, out var oldTranslationName))
            if (enableOverwrite)
                logger.LogInformationEx(
                    $"overwrite map {mapName} translation name: {oldTranslationName} -> {mapTranslationName}");
            else
                return;
        translationMapData.TranslationNames[mapName] = mapTranslationName;
    }

    private async void Initialize()
    {
#if DEBUG 
        if (DesignModeHelper.IsDesignMode)
            return; //NOT SUPPORT IN DESIGN MODE
#endif
        translationMapData = await persistence.Load<TranslationMapData>();
        Task.Run(OnAutoSaveDataTask).NoWait();
    }

    private async void OnAutoSaveDataTask()
    {
        while (true)
        {
            try
            {
                await persistence.Save(translationMapData);
                logger.LogDebugEx("TranslationMapData saved.");
                await Task.Delay(TimeSpan.FromSeconds(15));
            }
            catch (Exception e)
            {
                logger.LogErrorEx(e,$"TranslationMapData failed: {e.Message}");
            }
        }
    }
}