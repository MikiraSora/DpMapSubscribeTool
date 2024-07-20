using Avalonia;
using AvaloniaDialogs.Views;
using DpMapSubscribeTool.ViewModels.Dialogs;

namespace DpMapSubscribeTool.Views.Dialogs;

public partial class TemplateDialogView : BaseDialog
{
    public static readonly StyledProperty<DialogViewModelBase> MainPageContentProperty =
        AvaloniaProperty.Register<TemplateDialogView, DialogViewModelBase>(nameof(MainPageContent));

    public TemplateDialogView(DialogViewModelBase dialogViewModelBase)
    {
        MainPageContent = dialogViewModelBase;
        dialogViewModelBase?.SetDialogView(this);
        InitializeComponent();
        DataContext = this;
    }

    public DialogViewModelBase MainPageContent
    {
        get => GetValue(MainPageContentProperty);
        set => SetValue(MainPageContentProperty, value);
    }
}