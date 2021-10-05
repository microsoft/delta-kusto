@description('Tenant ID (for client id)')
param tenantId string
@description('Service Principal Client ID (which should be cluster admin)')
param clientId string

var uniqueId = uniqueString(resourceGroup().id, 'delta-kusto')
var clusterName = 'cluster${uniqueId}'
var prefixes = [
    'github_linux_'
    'github_win_'
    'github_mac_os_'
    ]
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
    name: 'main-admin'
    parent:  cluster
    properties: {
        principalId: clientId
        principalType: 'App'
        role: 'AllDatabasesAdmin'
        tenantId: tenantId
    }
}

resource db1 'Microsoft.Kusto/clusters/databases@2021-01-01' = [for i in range(0, dbCountPerPrefix): {
    name: '${cluster.name}/${prefixes[0]}${i}'
    location: resourceGroup().location
    kind: 'ReadWrite'
}]

resource db2 'Microsoft.Kusto/clusters/databases@2021-01-01' = [for i in range(0, dbCountPerPrefix): {
    name: '${cluster.name}/${prefixes[1]}${i}'
    location: resourceGroup().location
    kind: 'ReadWrite'
}]

resource db3 'Microsoft.Kusto/clusters/databases@2021-01-01' = [for i in range(0, dbCountPerPrefix): {
    name: '${cluster.name}/${prefixes[2]}${i}'
    location: resourceGroup().location
    kind: 'ReadWrite'
}]
