using DpMapSubscribeTool.Services.Persistences;
using DpMapSubscribeTool.Utils.Injections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DpMapSubscribeTool.Desktop.ServiceImplement.Persistences
{
    [RegisterInjectable(typeof(IPersistence))]
    public class DesktopPersistence : IPersistence
    {
        private readonly string savePath;
        private Dictionary<string, string> settingMap;

        public DesktopPersistence()
        {
            savePath = Path.Combine(Path.GetDirectoryName(typeof(DesktopPersistence).Assembly.Location),
                "setting.json");
        }

        private string GetKey<T>()
        {
            return typeof(T).FullName;
        }

        public async ValueTask Save<T>(T obj)
        {
            var key = GetKey<T>();

            settingMap[key] = JsonSerializer.Serialize(obj);
            var content = JsonSerializer.Serialize(settingMap);
            await File.WriteAllTextAsync(savePath, content);
        }

        public async ValueTask<T> Load<T>() where T : new()
        {
            var key = GetKey<T>();

            if (settingMap is null)
            {
                if (File.Exists(savePath))
                {
                    var content = await File.ReadAllTextAsync(savePath);
                    settingMap = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                }
                else
                {
                    settingMap = new Dictionary<string, string>();
                }
            }

            return settingMap.TryGetValue(key, out var jsonContent) ? JsonSerializer.Deserialize<T>(jsonContent) : new T();
        }
    }
}
