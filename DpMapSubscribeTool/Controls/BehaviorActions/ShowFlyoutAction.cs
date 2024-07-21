using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Xaml.Interactivity;

namespace DpMapSubscribeTool.Controls.BehaviorActions;

public class ShowFlyoutAction : AvaloniaObject, IAction
{
    public object Execute(object sender, object parameter)
    {
        if (sender is not Control attachedControl)
            return false;

        var flyout = attachedControl.GetValue(FlyoutBase.AttachedFlyoutProperty);
        flyout?.ShowAt(attachedControl);
        return true;
    }
}