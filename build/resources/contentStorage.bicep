param principalId string
param deploymentSuffix string
param location string = resourceGroup().location

var storageBlobDataContributor = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

resource contentStorage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
    name: 'liftledgercontent${deploymentSuffix}'
    location: location
    sku: {
        name: 'Standard_LRS'
    }
    kind: 'StorageV2'
    properties: {
        accessTier: 'Hot'
        allowBlobPublicAccess: false
        allowCrossTenantReplication: false
        allowSharedKeyAccess: true
        encryption: {
            keySource: 'Microsoft.Storage'
            requireInfrastructureEncryption: false
            services: { 
                blob: {
                    enabled: true
                    keyType: 'Account'
                }
                file: {
                    enabled: true
                    keyType: 'Account'
                }
                queue: {
                    enabled: true
                    keyType: 'Service'
                }
                table: {
                    enabled: true
                    keyType: 'Service'
                }
            }
        }
        isHnsEnabled: false
        isNfsV3Enabled: false
        keyPolicy: {
          keyExpirationPeriodInDays: 7
        }
        largeFileSharesState: 'Disabled'
        minimumTlsVersion: 'TLS1_2'
        networkAcls: {
          bypass: 'AzureServices'
          defaultAction: 'Deny'
        }
        supportsHttpsTrafficOnly: true
    }
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: contentStorage
  name: guid(contentStorage.id, storageBlobDataContributor, principalId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributor) 
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}