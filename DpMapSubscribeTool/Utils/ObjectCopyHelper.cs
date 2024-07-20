using System;
using System.Collections.Generic;
using System.Reflection;

namespace DpMapSubscribeTool.Utils;

public static class ObjectCopyHelper
{
    private static readonly Dictionary<int, Action<object, object>> cacheCopyFuncMap = new();

    public static void CopyProperties<FROM, TO>(FROM from, TO to)
    {
        var copyFunc = GetCopyFunc<FROM, TO>();
        copyFunc(from, to);
    }

    private static Action<object, object> GetCopyFunc<FROM, TO>()
    {
        var fromType = typeof(FROM);
        var toType = typeof(TO);
        var key = $"{fromType.Name}->{toType}".GetHashCode();
        if (cacheCopyFuncMap.TryGetValue(key, out var func))
            return func;

        var fromProperties = fromType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var toProperties = toType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var fromProperty in fromProperties)
            if (fromProperty.CanRead)
            {
                var toProperty = Array.Find(toProperties,
                    p => p.Name == fromProperty.Name && p.PropertyType == fromProperty.PropertyType && p.CanWrite);

                if (toProperty != null)
                {
                    Action<object, object> add = (from, to) =>
                    {
                        toProperty.SetValue(to, fromProperty.GetValue(from, null), null);
                    };
                    if (func is null)
                        func = add;
                    else
                        func += add;
                }
            }

        return cacheCopyFuncMap[key] = func;
    }
}