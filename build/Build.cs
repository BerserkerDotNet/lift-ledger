using System;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
[GitHubActions(
    "pr",
    GitHubActionsImage.UbuntuLatest,
    On = new [] { GitHubActionsTrigger.PullRequest },
    ImportSecrets = new []{ nameof(EncodedKeystore), nameof(AndroidSigningPassword)},
    InvokedTargets = new[] { nameof(SetupAndroidSDK), nameof(Compile), nameof(Test) })]
[GitHubActions(
    "continuos",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    OnPushBranches = new []{ "master" },
    ImportSecrets = new []{ nameof(AzureSPNCreds), nameof(EncodedKeystore), nameof(AndroidSigningPassword)},
    PublishArtifacts = true,
    InvokedTargets = new[] { nameof(SetupAndroidSDK), nameof(Compile), nameof(Test), nameof(PublishMobile), nameof(PublishAPI) })]
[GitHubActions(
    "deploy",
    GitHubActionsImage.UbuntuLatest,
    On = new [] { GitHubActionsTrigger.WorkflowDispatch },
    ImportSecrets = new []{ nameof(AzureSPNCreds), nameof(EncodedKeystore), nameof(AndroidSigningPassword)},
    InvokedTargets = new[] { nameof(SetupAndroidSDK), nameof(Compile), nameof(Test), nameof(PublishMobile), nameof(PublishAPI) },
    PublishArtifacts = true,
    AutoGenerate = false)]
partial class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.UpdateAzureContainerApp);

    [Solution(GenerateProjects = true)]
    readonly Solution Solution;
    
    readonly AbsolutePath ArtifactsDirectory = RootDirectory / "artifacts";
    
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Should publish test results to GitHub")]
    readonly bool PublishTestResults;

    [Secret]
    [Parameter("Base64 encoded keystore")]
    readonly string EncodedKeystore;
    
    [Secret]
    [Parameter("Signing password")]
    readonly string AndroidSigningPassword;
    
    [PathVariable("/usr/local/lib/android/sdk/cmdline-tools/latest/bin/sdkmanager")]
    readonly Tool SDKManager;
    
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DotNetClean(_ => _
                .SetProject(Solution));
        });

    Target SetupAndroidSDK => _ => _
        .Executes(() =>
        {
            DotNetWorkloadRestore(_ => _
                .SetProject(Solution));
            
            if (Host is GitHubActions)
            {
                SDKManager("platform-tools");
                var keystoreFile = Solution.src.LiftLedger_Mobile.Directory / "liftledger.keystore";
                keystoreFile.WriteAllBytes(Convert.FromBase64String(EncodedKeystore));
                Log.Information("keystore file decoded. {Path}", keystoreFile);
            }
        });
    
    Target Restore => _ => _
        .After(SetupAndroidSDK)
        .Executes(() =>
        {
            DotNetRestore(_ => _
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .After(SetupAndroidSDK)
        .Produces(ArtifactsDirectory / "*.binlog")
        .Executes(() =>
        {
            DotNetBuild(_ => _
                .EnableNoRestore()
                .SetConfiguration(Configuration)
                .SetBinaryLog("ledger.binlog")
                .SetProjectFile(Solution));
            foreach (var binLogFile in Solution.Directory.GlobFiles("*.binlog"))
            {
                binLogFile.CopyToDirectory(ArtifactsDirectory / "build-logs", ExistsPolicy.MergeAndOverwrite);
            }
        });
    
    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(_ => _
                    .EnableNoRestore()
                    .EnableNoBuild()
                    .SetConfiguration(Configuration)
                    .SetProjectFile(Solution)
                    .EnableCollectCoverage()
                    .SetCoverletOutput("cobertura")
                    .SetLoggers("trx")
                    .SetResultsDirectory(ArtifactsDirectory / "test-results"));
        });
}
