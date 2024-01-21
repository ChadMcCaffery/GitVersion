using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Agents;

internal class Jenkins : BuildAgentBase
{
    public const string EnvironmentVariableName = "JENKINS_URL";
    private string? file;
    protected override string EnvironmentVariable => EnvironmentVariableName;

    public Jenkins(IEnvironment environment, ILog log) : base(environment, log) => WithPropertyFile("gitversion.properties");

    public void WithPropertyFile(string propertiesFileName) => this.file = propertiesFileName;

    public override string GenerateSetVersionMessage(GitVersionVariables variables) => variables.FullSemVer;

    public override string[] GenerateSetParameterMessage(string name, string? value) =>
    [
        $"GitVersion_{name}={value}"
    ];

    public override string? GetCurrentBranch(bool usingDynamicRepos) => IsPipelineAsCode()
        ? Environment.GetEnvironmentVariable("BRANCH_NAME")
        : Environment.GetEnvironmentVariable("GIT_LOCAL_BRANCH") ?? Environment.GetEnvironmentVariable("GIT_BRANCH");

    private bool IsPipelineAsCode() => !Environment.GetEnvironmentVariable("BRANCH_NAME").IsNullOrEmpty();

    public override bool PreventFetch() => true;

    /// <summary>
    /// When Jenkins uses pipeline-as-code, it creates two remotes: "origin" and "origin1".
    /// This should be cleaned up, so that normalizing the Git repo will not fail.
    /// </summary>
    /// <returns></returns>
    public override bool ShouldCleanUpRemotes() => IsPipelineAsCode();

    public override void WriteIntegration(Action<string?> writer, GitVersionVariables variables, bool updateBuildNumber = true)
    {
        if (this.file is null)
            return;

        base.WriteIntegration(writer, variables, updateBuildNumber);
        writer($"Outputting variables to '{this.file}' ... ");
        File.WriteAllLines(this.file, GenerateBuildLogOutput(variables));
    }
}
