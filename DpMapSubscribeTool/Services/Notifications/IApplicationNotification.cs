using System;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Services.Notifications;

public interface IApplicationNotification
{
    void NofityServerForSubscribeMap(Server server, MapSubscribe subscribe, Action userComfirmCallback);
}