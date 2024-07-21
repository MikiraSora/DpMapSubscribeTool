using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DpMapSubscribeTool.ValueConverters;

public class BrushMapTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string map)
            return Brushes.White;

        var mapType = map.Split("_").FirstOrDefault()?.ToLower();
        return mapType switch
        {
            "ze" => Brushes.Chocolate,
            "ttt" => Brushes.CornflowerBlue,
            "mg" => Brushes.DarkKhaki,
            "bhop" or "kz" or "surf" => Brushes.DarkOrchid,
            _ => Brushes.White
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}