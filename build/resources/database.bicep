param principalId string
param deploymentSuffix string
param location string = resourceGroup().location

var sqlContributorRole = '00000000-0000-0000-0000-000000000002'
var dbOperator = '230815da-be43-4aae-9cb4-875f7bd000aa'

// Cosmos DB
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
    name: 'lift-ledger-data-${deploymentSuffix}'
    kind: 'GlobalDocumentDB'
    location: location
    properties: {
        databaseAccountOfferType: 'Standard'
        consistencyPolicy: {
            defaultConsistencyLevel: 'Session'
        }
        locations: [
            {
                locationName: location
                isZoneRedundant: false
                failoverPriority: 0
            }
        ]
        capabilities:[
            {
                name: 'EnableServerless'
            }
        ]
    }
}

resource sqlRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(sqlContributorRole, principalId, cosmosDbAccount.id)
  parent: cosmosDbAccount
  properties:{
    principalId: principalId
    roleDefinitionId: '/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.DocumentDB/databaseAccounts/${cosmosDbAccount.name}/sqlRoleDefinitions/${sqlContributorRole}'
    scope: cosmosDbAccount.id
  }
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: cosmosDbAccount
  name: guid(cosmosDbAccount.id, dbOperator, principalId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', dbOperator) 
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}