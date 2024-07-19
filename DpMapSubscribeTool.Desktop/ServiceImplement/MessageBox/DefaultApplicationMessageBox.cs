using System.Threading.Tasks;
using Avalonia.Controls;
using DpMapSubscribeTool.Desktop.ServiceImplement.Notifications;
using DpMapSubscribeTool.Services.MessageBox;
using DpMapSubscribeTool.Utils.Injections;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;

namespace DpMapSubscribeTool.Desktop.ServiceImplement.MessageBox;

[RegisterInjectable(typeof(IApplicationMessageBox))]
public class DefaultApplicationMessageBox : IApplicationMessageBox
{
    private readonly ILogger<DefaultApplicationNotification> logger;

    public DefaultApplicationMessageBox(ILogger<DefaultApplicationNotification> logger)
    {
        this.logger = logger;
    }

    public async Task ShowModalDialog(string content, DialogMessageType messageType = DialogMessageType.Info)
    {
        var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ButtonDefinitions = new[]
            {
                new ButtonDefinition {IsDefault = true, Name = "关闭"}
            },
            ContentMessage = content,
            ContentTitle = "ZE蛆王小助手",
            CanResize = false,
            Topmost = true,
            SystemDecorations = SystemDecorations.BorderOnly,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Icon = messageType switch
            {
                DialogMessageType.Info => Icon.Warning,
                DialogMessageType.Error => Icon.Error,
                _ => Icon.Question
            }
        });
        await box.ShowAsync();
    }

    public async Task<bool> ShowComfirmModalDialog(string content, string yesButtonContent, string noButtonContent)
    {
        var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ButtonDefinitions = new[]
            {
                new ButtonDefinition {Name = yesButtonContent},
                new ButtonDefinition {IsDefault = true, Name = noButtonContent}
            },
            ContentMessage = content,
            ContentTitle = "ZE蛆王小助手",
            CanResize = false,
            Topmost = true,
            SystemDecorations = SystemDecorations.BorderOnly,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        });

        return await box.ShowAsync() == yesButtonContent;
    }
}