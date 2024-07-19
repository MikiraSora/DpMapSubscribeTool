using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Services.Settings.DefaultImpl;

[RegisterInjectable(typeof(ISettingManager), ServiceLifetime.Singleton)]
public class DefaultSettingManager : ISettingManager
{
    //private readonly Dictionary<Type, object> cachedProxySettingObjectMap = new();
    private readonly Dictionary<Type, object> cachedRawSettingObjectMap = new();
    private readonly ILogger<DefaultSettingManager> logger;
    private readonly IPersistence persistence;

    public DefaultSettingManager(IPersistence persistence, ILogger<DefaultSettingManager> logger)
    {
        this.persistence = persistence;
        this.logger = logger;
    }

    public async Task<T> GetSetting<T>() where T : ISetting
    {
        /*
        var type = typeof(T);
        if (!cachedProxySettingObjectMap.TryGetValue(type, out var proxyObj))
        {
            var rawObj = await GetRawSettingObject<T>();
            proxyObj = cachedProxySettingObjectMap[type] = PropertyProxy<T>.Create(rawObj);
        }

        return (T) proxyObj;
        */
        var rawObj = await GetRawSettingObject<T>();
        return rawObj;
    }

    public async Task ResetSetting<T>() where T : ISetting
    {
        var type = typeof(T);
        cachedRawSettingObjectMap[type] = await persistence.Load<T>();
        logger.LogInformation($"Reset setting {type.FullName}");
    }

    public async Task SaveSetting<T>() where T : ISetting
    {
        var rawObj = await GetRawSettingObject<T>();
        await persistence.Save(rawObj);
        logger.LogInformation($"Saved setting {typeof(T).FullName}");
    }

    private async Task<T> GetRawSettingObject<T>() where T : ISetting
    {
        var type = typeof(T);
        if (!cachedRawSettingObjectMap.TryGetValue(type, out var obj))
        {
            obj = cachedRawSettingObjectMap[type] = await persistence.Load<T>();
            logger.LogInformation($"Get setting {typeof(T).FullName} from IPersistence");
        }

        return (T) obj;
    }

    public class PropertyProxy<T> : DispatchProxy
    {
        private T _originalObject;

        public static T Create(T originalObject)
        {
            object proxy = Create<T, PropertyProxy<T>>();
            ((PropertyProxy<T>) proxy)._originalObject = originalObject;
            return (T) proxy;
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (_originalObject == null)
                throw new ArgumentNullException(nameof(_originalObject));
            return targetMethod?.Invoke(_originalObject, args);
        }
    }
}