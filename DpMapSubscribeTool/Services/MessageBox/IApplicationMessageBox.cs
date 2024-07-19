using System.Threading.Tasks;

namespace DpMapSubscribeTool.Services.MessageBox;

public interface IApplicationMessageBox
{
    Task ShowModalDialog(string content, DialogMessageType messageType = DialogMessageType.Info);
    Task<bool> ShowComfirmModalDialog(string content, string yesButtonContent = "确认", string noButtonContent = "取消");
}