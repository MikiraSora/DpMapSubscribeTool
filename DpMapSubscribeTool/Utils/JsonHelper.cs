using System.Text.Json;

namespace DpMapSubscribeTool.Utils;

public static class JsonHelper
{
    public static T CopyNew<T>(T obj)
    {
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(obj));
    }
}