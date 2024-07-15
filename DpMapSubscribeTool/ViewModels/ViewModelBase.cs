using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DpMapSubscribeTool.ViewModels;

public class ViewModelBase : ObservableObject
{
    public virtual void OnViewAfterLoaded(Control view)
    {

    }

    public virtual void OnViewBeforeUnload(Control view)
    {

    }
}
