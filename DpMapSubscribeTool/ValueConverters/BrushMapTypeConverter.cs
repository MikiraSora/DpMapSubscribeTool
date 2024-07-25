using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.ValueConverters;

public class BrushMapTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not GameMode mode)
            return Brushes.White;

        return mode switch
        {
            GameMode.ZE => Brushes.Chocolate,
            GameMode.TTT => Brushes.CornflowerBlue,
            GameMode.MG => Brushes.DarkKhaki,
            GameMode.BHOP or GameMode.KZ or GameMode.SURF => Brushes.DarkOrchid,
            _ => Brushes.White
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}