using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Xaml.Interactivity;

namespace DpMapSubscribeTool.Controls.BehaviorActions;

public class ShowFlyoutAction : AvaloniaObject, IAction
{
    public static readonly StyledProperty<Control> FlyoutAttachedControlProperty =
        AvaloniaProperty.Register<ShowFlyoutAction, Control>(nameof(FlyoutAttachedControl));

    public Control FlyoutAttachedControl
    {
        get => GetValue(FlyoutAttachedControlProperty);
        set => SetValue(FlyoutAttachedControlProperty, value);
    }

    public object Execute(object sender, object parameter)
    {
        return TryFindFlyoutOpen(FlyoutAttachedControl) || TryFindFlyoutOpen(sender as Control);
    }

    private bool TryFindFlyoutOpen(Control control)
    {
        var flyout = control?.GetValue(FlyoutBase.AttachedFlyoutProperty);
        if (flyout is null)
            return false;
        flyout.ShowAt(control);
        return true;
    }
}