using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DpMapSubscribeTool.Models;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Utils;

namespace DpMapSubscribeTool.ViewModels.Dialogs.SqueezeJoinSetup;

public partial class SqueezeJoinSetupDialogViewModel : DialogViewModelBase
{
    private readonly IPersistence persistence;

    [ObservableProperty]
    private bool isComfirm;

    [ObservableProperty]
    private SqueezeJoinServerOption option;

    public SqueezeJoinSetupDialogViewModel()
    {
        DesignModeHelper.CheckOnlyForDesignMode();
    }

    public SqueezeJoinSetupDialogViewModel(IPersistence persistence)
    {
        this.persistence = persistence;

        Initialize();
    }

    public override string DialogIdentifier => nameof(SqueezeJoinSetupDialogViewModel);

    public override string Title => "自动挤服参数设置";

    private async void Initialize()
    {
        Option = await persistence.Load<SqueezeJoinServerOption>();
    }

    [RelayCommand]
    private void Cancel()
    {
        IsComfirm = false;
        CloseDialog();
    }

    [RelayCommand]
    private async Task Comfirm()
    {
        //save
        await persistence.Save(Option);

        IsComfirm = true;
        CloseDialog();
    }
}