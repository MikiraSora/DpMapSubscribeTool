using System.Threading.Tasks;

namespace DpMapSubscribeTool.Services.Map;

public interface IMapManager
{
    string GetMapTranslationName(string mapName);
    void CacheMapTranslationName(string mapName, string mapTranslationName,bool enableOverwrite);
}