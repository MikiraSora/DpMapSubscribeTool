using System.Threading.Tasks;

namespace DpMapSubscribeTool.Services.SteamAPI;

public interface ISteamAPIManager
{
    bool IsEnable { get; }

    Task<QueryUserNameResult> GetCurrentLoginUserName();

    Task<byte[]> GetMapThumbPictureImageData(string mapName);
}