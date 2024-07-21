using System;
using System.Threading.Tasks;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Utils.MethodExtensions;

namespace DpMapSubscribeTool.Services.Notifications;

public interface IApplicationNotification
{
    void NofityServerForSubscribeMap(Server server, MapSubscribe subscribe, Action<UserReaction> userComfirmCallback);
    void NofitySqueezeJoinSuccess(ServerInfo serverInfo, Action<UserReaction> userComfirmCallback);

    Task<UserReaction> NofityServerForSubscribeMap(Server server, MapSubscribe subscribe)
    {
        var taskCompleteSource = new TaskCompletionSource<UserReaction>();
        Task.Run(() => { NofityServerForSubscribeMap(server, subscribe, a => taskCompleteSource.SetResult(a)); })
            .NoWait();
        return taskCompleteSource.Task;
    }

    Task<UserReaction> NofitySqueezeJoinSuccess(ServerInfo serverInfo)
    {
        var taskCompleteSource = new TaskCompletionSource<UserReaction>();
        Task.Run(() => { NofitySqueezeJoinSuccess(serverInfo, a => taskCompleteSource.SetResult(a)); })
            .NoWait();
        return taskCompleteSource.Task;
    }
}