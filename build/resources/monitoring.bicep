param deploymentSuffix string
param location string = resourceGroup().location

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
    name: 'liftledger-analytics-workspace-${deploymentSuffix}'
    location: location
    properties: {
        sku: {
            name: 'PerGB2018'
        }
        retentionInDays: 30
        publicNetworkAccessForIngestion: 'Enabled'
        publicNetworkAccessForQuery: 'Enabled'
        workspaceCapping:{
            dailyQuotaGb: 1
        }
    }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
    name: 'liftledger-analytics-${deploymentSuffix}'
    location: location
    kind: 'web'
    properties: {
        WorkspaceResourceId: logAnalyticsWorkspace.id
        Flow_Type: 'Bluefield'
    }
}

output appInsightsConnectionString string = applicationInsights.properties.ConnectionString
