using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.ViewModels.Dialogs.AddNewCustomServerInfo;

public partial class AddNewCustomServerInfoDialogViewModel : DialogViewModelBase
{
    [ObservableProperty]
    private ServerInfo serverInfo;
    
    [ObservableProperty]
    private bool isComfirm;
    
    public override string DialogIdentifier => nameof(AddNewCustomServerInfoDialogViewModel);
    public override string Title => "手动添加新的服务器";

    [RelayCommand]
    private void Comfirm()
    {
        IsComfirm = true;
        CloseDialog();
    }
    
    [RelayCommand]
    private void Cancel()
    {
        IsComfirm = false;
        CloseDialog();
    }
}