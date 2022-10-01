using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using Microsoft.Extensions.Options;

namespace GitVersion;

public class GitVersionContextFactory : IGitVersionContextFactory
{
    private readonly IConfigProvider configProvider;
    private readonly IRepositoryStore repositoryStore;
    private readonly IOptions<GitVersionOptions> options;

    public GitVersionContextFactory(IConfigProvider configProvider, IRepositoryStore repositoryStore, IOptions<GitVersionOptions> options)
    {
        this.configProvider = configProvider.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
        this.options = options.NotNull();
    }

    public GitVersionContext Create(GitVersionOptions gitVersionOptions)
    {
        var currentBranch = this.repositoryStore.GetTargetBranch(gitVersionOptions.RepositoryInfo.TargetBranch);
        if (currentBranch == null)
            throw new InvalidOperationException("Need a branch to operate on");

        var currentCommit = this.repositoryStore.GetCurrentCommit(currentBranch, gitVersionOptions.RepositoryInfo.CommitId);

        var configuration = this.configProvider.Provide(this.options.Value.ConfigInfo.OverrideConfig);
        if (currentBranch.IsDetachedHead)
        {
            var branchForCommit = this.repositoryStore.GetBranchesContainingCommit(currentCommit, onlyTrackedBranches: gitVersionOptions.Settings.OnlyTrackedBranches).OnlyOrDefault();
            currentBranch = branchForCommit ?? currentBranch;
        }

        var currentCommitTaggedVersion = this.repositoryStore.GetCurrentCommitTaggedVersion(currentCommit, configuration.TagPrefix);
        var numberOfUncommittedChanges = this.repositoryStore.GetNumberOfUncommittedChanges();

        return new GitVersionContext(currentBranch, currentCommit, configuration, currentCommitTaggedVersion, numberOfUncommittedChanges);
    }
}
