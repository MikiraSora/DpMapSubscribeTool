using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DpMapSubscribeTool.Services.Map;
using DpMapSubscribeTool.Utils.Injections;

namespace DpMapSubscribeTool.ValueConverters;

[RegisterInjectable(typeof(IInjectableMultiValueConverter))]
public class GetMapTranslationNameConverter : IInjectableMultiValueConverter
{
    private readonly IMapManager mapManager;

    public GetMapTranslationNameConverter(IMapManager mapManager)
    {
        this.mapManager = mapManager;
    }

    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.ElementAtOrDefault(0) is string serverGroup && values.ElementAtOrDefault(1) is string mapName &&
            !string.IsNullOrWhiteSpace(mapName))
            return mapManager.GetMapTranslationName(serverGroup, mapName);
        return string.Empty;
    }
}