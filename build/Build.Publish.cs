using System;
using System.Linq;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

public sealed class AzureSPNCredentials
{
    public string SubscriptionId { get; set; }
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
}

partial class Build
{
    [Parameter("Acr login server")]
    readonly string AcrServer = "berserkerdotnetcregistry.azurecr.io";
    
    [Parameter("Suffix to add to Azure resources")]
    readonly string DeploymentSuffix = "test";
    
    [Parameter("Should update container app")]
    readonly bool PreventContainerAppUpdate = false;
    
    [Secret]
    [Parameter("Credentials for Azure SPN")]
    readonly string AzureSPNCreds;
    
    [PathVariable("az")]
    readonly Tool AzCli;
    
    Target PublishMobile => _ => _
        .After(Test)
        .Executes(() =>
        {
            DotNetPublish(_ => _
                .SetProject(Solution.src.LiftLedger_Mobile)
                .SetFramework("net9.0-android"));
        });

    Target AzLogin => _ => _
        .After(Test)
        .Requires(() => AzCli)
        .Requires(() => AcrServer)
        .Requires(() => AzureSPNCreds)
        .Executes(() =>
        {
            var creds = JsonSerializer.Deserialize<AzureSPNCredentials>(AzureSPNCreds, new JsonSerializerOptions{PropertyNameCaseInsensitive = true});
            
            AzCli($"login --service-principal -t {creds.TenantId} -u {creds.ClientId} -p {creds.ClientSecret}");
            AzCli($"account set -s {creds.SubscriptionId}");
            AzCli($"acr login -n {AcrServer}");
        });
    
    Target PublishAPI => _ => _
        .DependsOn(ProvisionAzureResources)
        .DependsOn(AzLogin)
        .After(Test)
        .Requires(() => AzCli)
        .Requires(() => DeploymentSuffix)
        .Requires(() => AcrServer)
        .Executes(() =>
        {
            DotNetPublish(_ => _
                .SetProject(Solution.src.LiftLedger_API)
                .DisableSelfContained()
                .SetRuntime("linux-x64")
                .SetPublishProfile("DefaultContainer")
                .SetProperty("ContainerRepository", $"lift-ledger-app-{DeploymentSuffix}")
                .SetProperty("ContainerImageTag", "latest") // Use ContainerImageTags to set both latest and specific version
                .SetProperty("ContainerRegistry", AcrServer));
        });
    
    Target UpdateAzureContainerApp => _ => _
        .OnlyWhenDynamic(() => !PreventContainerAppUpdate)
        .DependsOn(AzLogin)
        .DependsOn(ProvisionAzureResources)
        .TriggeredBy(PublishAPI)
        .Executes(() =>
        {
            var containerAppName = $"lift-ledger-app-{DeploymentSuffix}";
            AzCli("config set extension.use_dynamic_install=yes_without_prompt");
            AzCli($"containerapp up --name {containerAppName} --registry-server {AcrServer} --image {AcrServer}/{containerAppName}:latest");
        });
    
    Target ProvisionAzureResources => _ => _
        .DependsOn(AzLogin)
        .Requires(() => AzCli)
        .Requires(() => DeploymentSuffix)
        .Executes(() =>
        {
            var resourceGroup = $"lift-ledger-{DeploymentSuffix}";
            var resourceFile = RootDirectory / "build" / "resources" / "resources.bicep";
            AzCli($"deployment group create --resource-group {resourceGroup} --template-file {resourceFile} --parameters deploymentSuffix={DeploymentSuffix}");
        });
}