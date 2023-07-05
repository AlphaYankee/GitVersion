using GitVersion.Output.AssemblyInfo;
using GitVersion.Output.GitVersionInfo;
using GitVersion.Output.OutputGenerator;
using GitVersion.Output.WixUpdater;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Output;

public class GitVersionOutputModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<IGitVersionOutputTool, GitVersionOutputTool>();
        services.AddSingleton<IOutputGenerator, OutputGenerator.OutputGenerator>();
        services.AddSingleton<IGitVersionInfoGenerator, GitVersionInfoGenerator>();
        services.AddSingleton<IWixVersionFileUpdater, WixVersionFileUpdater>();
        services.AddSingleton<IAssemblyInfoFileUpdater, AssemblyInfoFileUpdater>();
        services.AddSingleton<IProjectFileUpdater, ProjectFileUpdater>();
    }
}
