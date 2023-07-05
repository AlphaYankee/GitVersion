using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal class EffectiveBranchConfigurationFinder : IEffectiveBranchConfigurationFinder
{
    private readonly ILog log;
    private readonly IRepositoryStore repositoryStore;

    public EffectiveBranchConfigurationFinder(ILog log, IRepositoryStore repositoryStore)
    {
        this.log = log.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
    }

    public virtual IEnumerable<EffectiveBranchConfiguration> GetConfigurations(IBranch branch, IGitVersionConfiguration configuration)
    {
        branch.NotNull();
        configuration.NotNull();

        return GetEffectiveConfigurationsRecursive(branch, configuration, null, new());
    }

    private IEnumerable<EffectiveBranchConfiguration> GetEffectiveConfigurationsRecursive(
        IBranch branch, IGitVersionConfiguration configuration, IBranchConfiguration? childBranchConfiguration, HashSet<IBranch> traversedBranches)
    {
        if (!traversedBranches.Add(branch)) yield break; // This should never happen!! But it is good to have a circuit breaker.

        var branchConfiguration = configuration.GetBranchConfiguration(branch);
        if (childBranchConfiguration != null)
        {
            branchConfiguration = childBranchConfiguration.Inherit(branchConfiguration);
        }

        var sourceBranches = Array.Empty<IBranch>();
        if (branchConfiguration.Increment == IncrementStrategy.Inherit)
        {
            // At this point we need to check if source branches are available.
            sourceBranches = this.repositoryStore.GetSourceBranches(branch, configuration, traversedBranches).ToArray();

            if (sourceBranches.Length == 0)
            {
                // Because the actual branch is marked with the inherit increment strategy we need to either skip the iteration or go further
                // while inheriting from the fallback branch configuration. This behavior is configurable via the increment settings of the configuration.
                var skipTraversingOfOrphanedBranches = configuration.Increment == IncrementStrategy.Inherit;
                this.log.Info(
                    $"An orphaned branch '{branch}' has been detected and will be skipped={skipTraversingOfOrphanedBranches}."
                );
                if (skipTraversingOfOrphanedBranches) yield break;
            }
        }

        if (branchConfiguration.Increment == IncrementStrategy.Inherit && sourceBranches.Any())
        {
            foreach (var sourceBranch in sourceBranches)
            {
                foreach (var effectiveConfiguration
                    in GetEffectiveConfigurationsRecursive(sourceBranch, configuration, branchConfiguration, traversedBranches))
                {
                    yield return effectiveConfiguration;
                }
            }
        }
        else
        {
            yield return new(branch, new EffectiveConfiguration(configuration, branchConfiguration));
        }
    }
}
