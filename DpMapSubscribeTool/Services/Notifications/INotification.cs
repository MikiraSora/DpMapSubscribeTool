using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DpMapSubscribeTool.Services.Notifications
{
    public interface INotification
    {
        IDisposableNotificationHolder BeginLoadingNotification(string message, out CancellationToken cancellationToken);

        void ShowInfomation(string message);
        void ShowWarnning(string message);
        void ShowError(Exception e, string message);
        void ShowError(string message);
    }
}
