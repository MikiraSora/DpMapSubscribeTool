using System;
using System.Globalization;
using Avalonia.Data.Converters;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.ValueConverters;

public class DisplayPlayerCountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Server server)
            return "未知";
        return server.MaxPlayerCount < 0 ? string.Empty : $"{server.CurrentPlayerCount}/{server.MaxPlayerCount}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}