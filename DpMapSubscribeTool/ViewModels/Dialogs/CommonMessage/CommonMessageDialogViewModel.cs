using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DpMapSubscribeTool.Services.Dialog;
using DpMapSubscribeTool.Utils;

namespace DpMapSubscribeTool.ViewModels.Dialogs.CommonMessage;

public partial class CommonMessageDialogViewModel : DialogViewModelBase
{
    [ObservableProperty]
    private string content;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private DialogMessageType dialogMessageType;

    public CommonMessageDialogViewModel()
    {
        DesignModeHelper.CheckOnlyForDesignMode();
    }

    public CommonMessageDialogViewModel(DialogMessageType dialogMessageType, string content)
    {
        this.dialogMessageType = dialogMessageType;
        this.content = content;
    }

    public override string DialogIdentifier => nameof(CommonMessageDialogViewModel);

    public override string Title => DialogMessageType switch
    {
        DialogMessageType.Error => "错误",
        DialogMessageType.Info or _ => "消息"
    };

    [RelayCommand]
    private void Close()
    {
        CloseDialog();
    }
}