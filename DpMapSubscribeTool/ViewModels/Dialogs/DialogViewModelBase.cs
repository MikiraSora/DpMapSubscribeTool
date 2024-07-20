using AvaloniaDialogs.Views;

namespace DpMapSubscribeTool.ViewModels.Dialogs;

public abstract class DialogViewModelBase : ViewModelBase
{
    private BaseDialog dialogView;
    public abstract string DialogIdentifier { get; }
    public abstract string Title { get; }

    public void CloseDialog()
    {
        dialogView?.Close();
    }

    internal void SetDialogView(BaseDialog dialogView)
    {
        this.dialogView = dialogView;
    }
}