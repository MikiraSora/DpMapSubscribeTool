using System;
using System.Threading;
using System.Threading.Tasks;

namespace DpMapSubscribeTool.Services.SteamAPI;

public interface ISteamAPIManager
{
    bool IsEnable { get; }

    Task<QueryUserNameResult> GetCurrentLoginUserName();

    Task<byte[]> GetMapThumbPictureImageData(string mapName);

    Task<QuestServerResult> QueryServer(string host, int port, CancellationToken ct);

    Task<bool> CheckIfCS2LogParsingEnable();

    event Action<NetworkDisconnectionReason> OnJoinServerFailed;
}