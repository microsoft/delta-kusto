/**************************************************/
//  Deploy ADX clusters with multiple databases
//  ready to accomodate integration tests

var intTestDbCountPerPrefix = 100

var uniqueId = uniqueString(resourceGroup().id, 'delta-kusto')
var prefixes = [
  'github_linux_'
  'github_win_'
  'github_mac_os_'
  'github_laptop_'
]

resource intTestCluster 'Microsoft.Kusto/clusters@2022-12-29' = {
  name: 'intTests${uniqueId}'
  location: resourceGroup().location
  tags: {
    'testLevel': 'integration'
  }
  sku: {
    'name': 'Dev(No SLA)_Standard_E2a_v4'
    'tier': 'Basic'
    'capacity': 1
  }
  properties: {
    enableStreamingIngest: true
  }
}

@batchSize(5)
resource intTestDbs 'Microsoft.Kusto/clusters/databases@2022-12-29' = [for i in range(0, length(prefixes) * intTestDbCountPerPrefix): {
  name: '${prefixes[i / intTestDbCountPerPrefix]}${format('{0:D8}', i % intTestDbCountPerPrefix + 1)}'
  location: resourceGroup().location
  parent: intTestCluster
  kind: 'ReadWrite'
}]
