using System;
using System.Globalization;
using Avalonia.Data.Converters;
using DpMapSubscribeTool.Services.Map;
using DpMapSubscribeTool.Utils.Injections;

namespace DpMapSubscribeTool.ValueConverters;

[RegisterInjectable(typeof(IInjectableValueConverter))]
public class GetMapTranslationNameConverter : IInjectableValueConverter
{
    private readonly IMapManager mapManager;

    public GetMapTranslationNameConverter(IMapManager mapManager)
    {
        this.mapManager = mapManager;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is not string mapName ? string.Empty : mapManager.GetMapTranslationName(mapName);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}