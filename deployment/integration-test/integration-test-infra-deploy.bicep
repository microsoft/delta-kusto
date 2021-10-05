@description('Tenant ID (for client id)')
param tenantId string
@description('Service Principal Client ID (which should be cluster admin)')
param clientId string

var uniqueId = uniqueString(resourceGroup().id, 'delta-kusto')
var clusterName = 'cluster_${uniqueId}'
var linuxDbPrefix = 'github_linux_'
var dbCountPerPrefix = 100

resource cluster 'Microsoft.Kusto/clusters@2021-01-01' = {
    name: clusterName
    location: resourceGroup().location
    tags: {}
    sku: {
        'name': 'Dev(No SLA)_Standard_E2a_v4'
        'tier': 'Basic'
        'capacity': 1
    }
}

resource admin 'Microsoft.Kusto/clusters/principalAssignments@2021-01-01' = {
    name: '${cluster.name}/main-admin'
    properties: {
      principalId: clientId
      principalType: 'App'
      role: 'AllDatabasesAdmin'
      tenantId: tenantId
    }
  }

resource db 'Microsoft.Kusto/clusters/databases@2021-01-01' = [for i in range(0, dbCountPerPrefix): {
    name: '${cluster.name}/${linuxDbPrefix}-${i}'
    location: resourceGroup().location
    kind: 'ReadWrite'
}]
