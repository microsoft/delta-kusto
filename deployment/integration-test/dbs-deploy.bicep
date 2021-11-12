/**************************************************/
//  Deploy multiple ADX databases

@description('Name of the cluster to deploy DBs under')
param clusterName string
@description('Database indices')
param dbIndices array
@description('Prefix of databases')
param dbPrefix string

resource cluster 'Microsoft.Kusto/clusters@2021-01-01' existing = {
  name: clusterName
}

@batchSize(30)
resource perfTestDbs 'Microsoft.Kusto/clusters/databases@2021-01-01' = [for i in dbIndices: {
  name: '${dbPrefix}${format('{0:D8}', i + 1)}'
  location: resourceGroup().location
  parent: cluster
  kind: 'ReadWrite'
}]
