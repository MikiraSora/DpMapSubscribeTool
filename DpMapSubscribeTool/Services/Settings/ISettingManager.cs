using System.Threading.Tasks;

namespace DpMapSubscribeTool.Services.Settings;

public interface ISettingManager
{
    Task<T> GetSetting<T>() where T : ISetting;

    Task ResetSetting<T>() where T : ISetting;
    Task SaveSetting<T>() where T : ISetting;
}