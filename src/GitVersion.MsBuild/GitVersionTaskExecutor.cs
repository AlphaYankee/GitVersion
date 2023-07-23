using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.MsBuild.Tasks;
using GitVersion.OutputVariables;
using Microsoft.Extensions.Options;

namespace GitVersion.MsBuild;

internal class GitVersionTaskExecutor : IGitVersionTaskExecutor
{
    private readonly IFileSystem fileSystem;
    private readonly IGitVersionOutputTool gitVersionOutputTool;
    private readonly IConfigurationProvider configurationProvider;
    private readonly IOptions<GitVersionOptions> options;

    public GitVersionTaskExecutor(IFileSystem fileSystem, IGitVersionOutputTool gitVersionOutputTool,
                                  IConfigurationProvider configurationProvider, IOptions<GitVersionOptions> options)
    {
        this.fileSystem = fileSystem.NotNull();
        this.gitVersionOutputTool = gitVersionOutputTool.NotNull();
        this.configurationProvider = configurationProvider.NotNull();
        this.options = options.NotNull();
    }

    public void GetVersion(GetVersion task)
    {
        var versionVariables = VersionVariablesHelper.FromFile(task.VersionFile, fileSystem);
        var outputType = typeof(GetVersion);
        foreach (var (key, value) in versionVariables)
        {
            outputType.GetProperty(key)?.SetValue(task, value, null);
        }
    }

    public void UpdateAssemblyInfo(UpdateAssemblyInfo task)
    {
        var versionVariables = VersionVariablesHelper.FromFile(task.VersionFile, fileSystem);
        FileHelper.DeleteTempFiles();
        FileHelper.CheckForInvalidFiles(task.CompileFiles, task.ProjectFile);

        if (!string.IsNullOrEmpty(task.IntermediateOutputPath))
        {
            // Ensure provided output path exists first. Fixes issue #2815.
            fileSystem.CreateDirectory(task.IntermediateOutputPath);
        }

        var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(task.Language, task.ProjectFile, "AssemblyInfo");
        task.AssemblyInfoTempFilePath = PathHelper.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

        var gitVersionOptions = this.options.Value;
        gitVersionOptions.WorkingDirectory = fileWriteInfo.WorkingDirectory;
        gitVersionOptions.AssemblySettingsInfo.UpdateAssemblyInfo = true;
        gitVersionOptions.AssemblySettingsInfo.EnsureAssemblyInfo = true;
        gitVersionOptions.AssemblySettingsInfo.Files.Add(fileWriteInfo.FileName);

        gitVersionOutputTool.UpdateAssemblyInfo(versionVariables);
    }

    public void GenerateGitVersionInformation(GenerateGitVersionInformation task)
    {
        var versionVariables = VersionVariablesHelper.FromFile(task.VersionFile, fileSystem);

        if (!string.IsNullOrEmpty(task.IntermediateOutputPath))
        {
            // Ensure provided output path exists first. Fixes issue #2815.
            fileSystem.CreateDirectory(task.IntermediateOutputPath);
        }

        var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(task.Language, task.ProjectFile, "GitVersionInformation");
        task.GitVersionInformationFilePath = PathHelper.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

        var gitVersionOptions = this.options.Value;
        gitVersionOptions.WorkingDirectory = fileWriteInfo.WorkingDirectory;
        var targetNamespace = getTargetNamespace(task);
        gitVersionOutputTool.GenerateGitVersionInformation(versionVariables, fileWriteInfo, targetNamespace);

        static string? getTargetNamespace(GenerateGitVersionInformation task)
        {
            string? targetNamespace = null;
            if (string.Equals(task.UseProjectNamespaceForGitVersionInformation, "true", StringComparison.OrdinalIgnoreCase))
            {
                targetNamespace = task.RootNamespace;
                if (string.IsNullOrWhiteSpace(targetNamespace))
                {
                    targetNamespace = Path.GetFileNameWithoutExtension(task.ProjectFile);
                }
            }

            return targetNamespace;
        }
    }

    public void WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task)
    {
        var versionVariables = VersionVariablesHelper.FromFile(task.VersionFile, fileSystem);

        var gitVersionOptions = this.options.Value;
        var configuration = this.configurationProvider.Provide(gitVersionOptions.ConfigurationInfo.OverrideConfiguration);

        gitVersionOutputTool.OutputVariables(versionVariables, configuration.UpdateBuildNumber);
    }
}
