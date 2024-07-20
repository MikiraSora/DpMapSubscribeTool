using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using DpMapSubscribeTool.Utils;
using DpMapSubscribeTool.Utils.Injections;
using DpMapSubscribeTool.ViewModels.Dialogs;
using DpMapSubscribeTool.ViewModels.Dialogs.CommonMessage;
using DpMapSubscribeTool.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DpMapSubscribeTool.Services.Dialog.DefaultImpl;

[RegisterInjectable(typeof(IDialogManager), ServiceLifetime.Singleton)]
public class DefaultDialogManager : IDialogManager
{
    private readonly Stack<Window> dialogWindowStack = new();
    private readonly ILogger<DefaultDialogManager> logger;
    private readonly ViewModelFactory viewModelFactory;

    public DefaultDialogManager(ViewModelFactory viewModelFactory, ILogger<DefaultDialogManager> logger)
    {
        this.viewModelFactory = viewModelFactory;
        this.logger = logger;
    }

    public Task<T> ShowDialog<T>() where T : DialogViewModelBase
    {
        var viewModel = viewModelFactory.CreateViewModel<T>();
        return ShowDialog(viewModel);
    }

    public async Task ShowMessageDialog(string content, DialogMessageType messageType = DialogMessageType.Info)
    {
        var vm = new CommonMessageDialogViewModel(messageType, content);
        await ShowDialog(vm);
    }

    public async Task<bool> ShowComfirmDialog(string content, string yesButtonContent = "确认",
        string noButtonContent = "取消")
    {
        var vm = new CommonComfirmDialogViewModel(content, yesButtonContent, noButtonContent);
        await ShowDialog(vm);
        return vm.ComfirmResult;
    }

    private async Task<T> ShowDialog<T>(T viewModel) where T : DialogViewModelBase
    {
        logger.LogInformation($"dialog {viewModel.DialogIdentifier} started.");
        var view = new TemplateDialogView(viewModel);
        await view.ShowAsync();
        logger.LogInformation($"dialog {viewModel.DialogIdentifier} finished");
        return viewModel;
    }
}