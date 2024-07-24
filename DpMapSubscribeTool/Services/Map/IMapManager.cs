namespace DpMapSubscribeTool.Services.Map;

public interface IMapManager
{
    string GetMapTranslationName(string serviceGroup, string mapName);
    void CacheMapTranslationName(string serviceGroup, string mapName, string mapTranslationName);
}