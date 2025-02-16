// CDN
    // With Storage
// Container apps
    // Access to CDN
// App Insights

@allowed(['test', 'prod'])
param deploymentSuffix string = 'test'

@description('Location for the resources.')
param location string = resourceGroup().location

module monitoring 'monitoring.bicep' = {
  name: 'lift-ledger-monitoring'
  params: {
    deploymentSuffix: deploymentSuffix
    location: location
  }
}

// Container App

resource liftledgerEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
    name: 'lift-ledger-${deploymentSuffix}-environment'
    location: location
    properties:{
        }
    }

resource liftledgerApp 'Microsoft.App/containerApps@2023-05-01' = {
    name: 'lift-ledger-app-${deploymentSuffix}'
    location: location
    identity:{
        type: 'SystemAssigned'
    }
    properties:{
        environmentId: liftledgerEnv.id
        configuration:{
            activeRevisionsMode: 'Single'
            secrets:[
                { 
                    name: 'appinsights-connectionstring'
                    value: monitoring.outputs.appInsightsConnectionString
                }
            ]
            ingress: {
                external: true
                transport: 'auto'
                allowInsecure: false
                targetPort: 8080
                stickySessions: {
                    affinity: 'none'
                }
            }
        }
        template:{
            containers: [{
                name: 'lift-ledger-app-${deploymentSuffix}'
                image: 'mcr.microsoft.com/azuredocs/aci-helloworld'
                resources: {
                    cpu: json('.5')
                    memory: '1Gi'
                    }
                }]
            scale: {
                minReplicas:0
                maxReplicas:1
            }
        }
    }
}

module acrRoleAssignment 'genericAcr.bicep' = {
  name: 'container-registry-role-assignment'
  scope: resourceGroup('generic')
  params: {
    principalId: liftledgerApp.identity.principalId
  }
}

module database 'database.bicep' = {
  name: 'lift-ledger-database'
  params: {
    principalId: liftledgerApp.identity.principalId
    deploymentSuffix: deploymentSuffix
    location: location
  }
}

module content 'contentStorage.bicep' = {
  name: 'lift-ledger-content'
  params: {
    principalId: liftledgerApp.identity.principalId
    deploymentSuffix: deploymentSuffix
    location: location
  }
}
