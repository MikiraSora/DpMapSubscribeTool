using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DpMapSubscribeTool.ValueConverters;

public class BrushPlayerCountConverter : IMultiValueConverter
{
    private static readonly Dictionary<Color, IBrush> cachedBrush = new();
    private static readonly Color progress30Color = Color.FromRgb(0, 255, 25);
    private static readonly Color progress50Color = Color.FromRgb(255, 255, 0);
    private static readonly Color progress64Color = Color.FromRgb(255, 0, 0);

    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.ElementAtOrDefault(0) is not int current || values.ElementAtOrDefault(1) is not int max)
            return BindingOperations.DoNothing;

        //Scale to 64.
        var progress = (int) (current * 1.0 / max * 64);

        var color = progress switch
        {
            <= 0 => Colors.White,
            <= 30 => CalculateGradientColor(0, 30, Colors.White, progress30Color, progress),
            <= 50 => CalculateGradientColor(30, 50, progress30Color, progress50Color, progress),
            _ => CalculateGradientColor(50, 64, progress50Color, progress64Color, progress)
        };

        if (!cachedBrush.TryGetValue(color, out var brush))
            brush = cachedBrush[color] = new SolidColorBrush(color);

        return brush;
    }

    private static Color CalculateGradientColor(int minValue, int maxValue, Color minColor, Color maxColor,
        int currentValue)
    {
        if (minValue == maxValue)
            return minColor;

        currentValue = Math.Max(minValue, Math.Min(maxValue, currentValue));

        var ratio = (float) (currentValue - minValue) / (maxValue - minValue);

        var r = (byte) (minColor.R + (maxColor.R - minColor.R) * ratio);
        var g = (byte) (minColor.G + (maxColor.G - minColor.G) * ratio);
        var b = (byte) (minColor.B + (maxColor.B - minColor.B) * ratio);

        return Color.FromArgb(255, r, g, b);
    }
}