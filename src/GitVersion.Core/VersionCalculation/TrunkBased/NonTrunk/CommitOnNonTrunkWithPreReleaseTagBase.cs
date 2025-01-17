using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.TrunkBased.NonTrunk;

internal abstract class CommitOnNonTrunkWithPreReleaseTagBase : ITrunkBasedIncrementer
{
    public virtual bool MatchPrecondition(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
        => !commit.HasChildIteration && !commit.Configuration.IsMainBranch
            && context.SemanticVersion?.IsPreRelease == true;

    public virtual IEnumerable<BaseVersionV2> GetIncrements(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        context.BaseVersionSource = commit.Value;

        yield return BaseVersionV2.ShouldIncrementFalse(
            source: GetType().Name,
            baseVersionSource: context.BaseVersionSource,
            semanticVersion: context.SemanticVersion.NotNull()
        );

        context.Increment = commit.GetIncrementForcedByBranch();
        context.Label = commit.Configuration.GetBranchSpecificLabel(commit.BranchName, null);
    }
}
