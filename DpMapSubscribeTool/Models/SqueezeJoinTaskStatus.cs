using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DpMapSubscribeTool.Models;

public partial class SqueezeJoinTaskStatus : ObservableObject
{
    private readonly WeakReference<PropertyChangedEventHandler> _weakHandler;

    [ObservableProperty]
    private SqueezeJoinServerOption option;

    [ObservableProperty]
    private Server server;

    public SqueezeJoinTaskStatus(Server server, SqueezeJoinServerOption option)
    {
        this.server = server;
        this.option = option;

        var handler = new PropertyChangedEventHandler(OnServerOrOptionPropertyChanged);
        _weakHandler = new WeakReference<PropertyChangedEventHandler>(handler);

        server.PropertyChanged += HandlerWrapper;
        option.PropertyChanged += HandlerWrapper;
    }

    private int RemainPlayerCountWaiting =>
        Server.CurrentPlayerCount - (Server.MaxPlayerCount - Option.SqueezeTargetPlayerCountDiff);

    private void HandlerWrapper(object sender, PropertyChangedEventArgs e)
    {
        if (_weakHandler.TryGetTarget(out var handler))
        {
            handler(sender, e);
        }
        else
        {
            if (sender is INotifyPropertyChanged publisher)
                publisher.PropertyChanged -= HandlerWrapper;
        }
    }

    private void OnServerOrOptionPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        switch (propertyChangedEventArgs.PropertyName)
        {
            case nameof(Server.MaxPlayerCount):
            case nameof(Option.SqueezeTargetPlayerCountDiff):
            case nameof(Server.CurrentPlayerCount):
                OnPropertyChanged(nameof(RemainPlayerCountWaiting));
                break;
        }
    }
}