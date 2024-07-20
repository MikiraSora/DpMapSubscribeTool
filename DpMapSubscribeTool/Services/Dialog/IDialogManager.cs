using System.Threading.Tasks;
using DpMapSubscribeTool.ViewModels;
using DpMapSubscribeTool.ViewModels.Dialogs;

namespace DpMapSubscribeTool.Services.Dialog;

public interface IDialogManager
{
    Task<T> ShowDialog<T>() where T : DialogViewModelBase;
    Task ShowMessageDialog(string content, DialogMessageType messageType = DialogMessageType.Info);
    Task<bool> ShowComfirmDialog(string content, string yesButtonContent = "确认", string noButtonContent = "取消");
}