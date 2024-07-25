using System;

namespace DpMapSubscribeTool.ViewModels.Pages.Home;

public class HomePageViewModel : PageViewModelBase
{
    public override string Title => "主页";

    public string ProgramCommitId => ThisAssembly.GitCommitId;
    public string ProgramCommitIdShort => ProgramCommitId[..7];
    public string AssemblyVersion => ThisAssembly.AssemblyVersion;
    public DateTime ProgramCommitDate => ThisAssembly.GitCommitDate + TimeSpan.FromHours(8);
    public string ProgramBuildConfiguration => ThisAssembly.AssemblyConfiguration;

    public string ProgramBuildTime
    {
        get
        {
            var type = typeof(HomePageViewModel).Assembly.GetType("DpMapSubscribeTool.BuildTime");
            var prop = type?.GetField("Value")?.GetValue(null)
                ?.ToString();
            return prop;
        }
    }
}