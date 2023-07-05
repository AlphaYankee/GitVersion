using GitVersion.Agents;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Output;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Core.Tests.Helpers;

public class GitVersionCoreTestModule : IGitVersionModule
{
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddModule(new GitVersionLibGit2SharpModule());
        services.AddModule(new GitVersionBuildAgentsModule());
        services.AddModule(new GitVersionOutputModule());
        services.AddModule(new GitVersionCoreModule());

        services.AddSingleton<IFileSystem, TestFileSystem>();
        services.AddSingleton<IEnvironment, TestEnvironment>();
        services.AddSingleton<ILog, NullLog>();
    }
}
