using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace DpMapSubscribeTool.ValueConverters;

public class DisplayPlayerCountConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.ElementAtOrDefault(0) is not int cur)
            return string.Empty;
        if (values.ElementAtOrDefault(1) is not int max)
            return string.Empty;
        return max < 0 ? string.Empty : $"{cur}/{max}";
    }
}