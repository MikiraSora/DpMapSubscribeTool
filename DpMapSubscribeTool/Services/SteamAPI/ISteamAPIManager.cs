using System.Threading.Tasks;

namespace DpMapSubscribeTool.Services.SteamAPI;

public interface ISteamAPIManager
{
    bool IsEnable { get; }

    Task<QueryUserNameResult> GetCurrentLoginUserName();
}