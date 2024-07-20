using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DpMapSubscribeTool.ViewModels.Dialogs.Hello;

public partial class HelloDialogViewModel : DialogViewModelBase
{
    [ObservableProperty]
    private string content;

    public override string DialogIdentifier => nameof(HelloDialogViewModel);
    public override string Title => "HLLLLLLLLLLLLLLLLLO";

    [RelayCommand]
    private void Close()
    {
        CloseDialog();
    }
}