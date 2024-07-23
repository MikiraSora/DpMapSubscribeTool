using System.Collections.Generic;
using System.Collections.ObjectModel;
using DpMapSubscribeTool.Models;

namespace DpMapSubscribeTool.Services.Persistences;

public class CustomServerSettings
{
    public ObservableCollection<ServerInfo> CustomServerInfos { get; set; } = new();
}