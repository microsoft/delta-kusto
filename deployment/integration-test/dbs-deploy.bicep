/**************************************************/
//  Deploy multiple ADX databases

@description('Name of the cluster to deploy DBs under')
param clusterName string
@description('Array of DB names')
param dbNames array

resource cluster 'Microsoft.Kusto/clusters@2021-01-01' existing = {
  name: clusterName
}

@batchSize(100)
resource perfTestDbs 'Microsoft.Kusto/clusters/databases@2021-01-01' = [for dbName in dbNames: {
  name: dbName
  location: resourceGroup().location
  parent: cluster
  kind: 'ReadWrite'
}]
