/**************************************************/
//  Deploy ADX clusters with multiple databases
//  ready to accomodate integration tests
//
//  A Logic App is deployed to shutdown the clusters
//  within 2-5 hours

@description('Tenant ID (for client id)')
param tenantId string
@description('Service Principal Client ID (which should be cluster admin)')
param clientId string

var intTestDbCountPerPrefix = 120
var perfTestDbCount = 1000
var perfTestIndices = range(0, perfTestDbCount)
var perfPartitionMaxSize = 800
var uniqueId = uniqueString(resourceGroup().id, 'delta-kusto')
var prefixes = [
  'github_linux_'
  'github_win_'
  'github_mac_os_'
  'github_laptop_'
]

resource intTestCluster 'Microsoft.Kusto/clusters@2021-01-01' = {
  name: 'intTests${uniqueId}'
  location: resourceGroup().location
  tags: {
    'autoShutdown': 'true'
    'testLevel': 'integration'
  }
  sku: {
    'name': 'Dev(No SLA)_Standard_E2a_v4'
    'tier': 'Basic'
    'capacity': 1
  }
}

@batchSize(25)
resource intTestDbs 'Microsoft.Kusto/clusters/databases@2021-01-01' = [for i in range(0, length(prefixes) * intTestDbCountPerPrefix): {
  name: '${prefixes[i / intTestDbCountPerPrefix]}${format('{0:D8}', i % intTestDbCountPerPrefix)}'
  location: resourceGroup().location
  parent: intTestCluster
  kind: 'ReadWrite'
}]

resource perfTestCluster 'Microsoft.Kusto/clusters@2021-01-01' = {
  name: 'perfTests${uniqueId}'
  location: resourceGroup().location
  tags: {
    'autoShutdown': 'true'
    'testLevel': 'perf'
  }
  sku: {
    'name': 'Dev(No SLA)_Standard_E2a_v4'
    'tier': 'Basic'
    'capacity': 1
  }

  resource admin 'principalAssignments' = {
    name: 'main-admin'
    properties: {
      principalId: clientId
      principalType: 'App'
      role: 'AllDatabasesAdmin'
      tenantId: tenantId
    }
  }
}

//  Delegate to a module to work around the 800 resources per deployment limitation
@batchSize(1)
module perfTestDbs 'dbs-deploy.bicep' = [for i in range(0, perfTestDbCount / perfPartitionMaxSize): {
  name: 'kustoDbs-${i}-${deployment().name}'
  params: {
    clusterName: perfTestCluster.name
    dbNames: take(skip(perfTestIndices, i * perfPartitionMaxSize), perfPartitionMaxSize)
  }
}]

resource autoShutdown 'Microsoft.Logic/workflows@2019-05-01' = {
  name: 'shutdownWorkflow${uniqueId}'
  location: resourceGroup().location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    state: 'Enabled'
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      parameters: {}
      triggers: {
        Recurrence: {
          recurrence: {
            frequency: 'Hour'
            interval: 2
          }
          evaluatedRecurrence: {
            frequency: 'Hour'
            interval: 2
          }
          type: 'Recurrence'
        }
      }
      actions: {
        'for-each-cluster': {
          foreach: '@body(\'get-clusters\').value'
          actions: {
            'if-should-shut-down': {
              actions: {
                'stop-cluster': {
                  runAfter: {
                    wait: [
                      'Succeeded'
                    ]
                  }
                  type: 'Http'
                  inputs: {
                    authentication: {
                      audience: environment().authentication.audiences[0]
                      type: 'ManagedServiceIdentity'
                    }
                    method: 'POST'
                    uri: '@{outputs(\'stop-cluster-url\')}'
                  }
                }
                'stop-cluster-url': {
                  runAfter: {}
                  type: 'Compose'
                  inputs: '@concat(\'${environment().resourceManager}subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Kusto/clusters/\', body(\'parse-payload\')?[\'name\'], \'/stop?api-version=2021-01-01\')'
                }
                wait: {
                  runAfter: {
                    'stop-cluster-url': [
                      'Succeeded'
                    ]
                  }
                  type: 'Wait'
                  inputs: {
                    interval: {
                      count: 1
                      unit: 'Hour'
                    }
                  }
                }
              }
              runAfter: {
                'parse-payload': [
                  'Succeeded'
                ]
              }
              expression: {
                and: [
                  {
                    equals: [
                      '@body(\'parse-payload\')?[\'tags\']?[\'autoShutdown\']'
                      'true'
                    ]
                  }
                  {
                    equals: [
                      '@body(\'parse-payload\')?[\'properties\']?[\'state\']'
                      'Running'
                    ]
                  }
                ]
              }
              type: 'If'
            }
            'parse-payload': {
              runAfter: {}
              type: 'ParseJson'
              inputs: {
                content: '@items(\'for-each-cluster\')'
                schema: {
                  properties: {
                    id: {
                      type: 'string'
                    }
                    name: {
                      type: 'string'
                    }
                    properties: {
                      properties: {
                        state: {
                          type: 'string'
                        }
                      }
                      type: 'object'
                    }
                    tags: {
                      properties: {
                        'auto-shutdown': {
                          type: 'string'
                        }
                      }
                      type: 'object'
                    }
                  }
                  type: 'object'
                }
              }
            }
          }
          runAfter: {
            'get-clusters': [
              'Succeeded'
            ]
          }
          type: 'Foreach'
          runtimeConfiguration: {
            concurrency: {
              repetitions: 50
            }
          }
        }
        'get-clusters': {
          runAfter: {}
          type: 'Http'
          inputs: {
            authentication: {
              audience: environment().authentication.audiences[0]
              type: 'ManagedServiceIdentity'
            }
            method: 'GET'
            uri: '${environment().resourceManager}subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Kusto/clusters?api-version=2021-01-01'
          }
        }
      }
      outputs: {}
    }
  }
}

//  Authorize WF as contributor on clusters
//  Role list:  https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
var contributorId = 'b24988ac-6180-42a0-ab88-20f7382dd24c'
var fullContributorId = '/subscriptions/${subscription().subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/${contributorId}'
var intTestClusterRoleAssignmentName = '${resourceGroup().id}${autoShutdown.name}${fullContributorId}${intTestCluster.name}'
var perfTestClusterRoleAssignmentName = '${resourceGroup().id}${autoShutdown.name}${fullContributorId}${perfTestCluster.name}'

resource autoShutdownIntTestClusterAuthorization 'Microsoft.Authorization/roleAssignments@2021-04-01-preview' = {
  name: '${guid(intTestClusterRoleAssignmentName)}'
  //  See https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/scope-extension-resources
  //  for scope for extension
  scope: intTestCluster
  properties: {
    description: 'Give contributor on the cluster'
    principalId: autoShutdown.identity.principalId
    //  Fix the issue of the principal not being ready when deployment the assignment
    principalType: 'ServicePrincipal'
    roleDefinitionId: fullContributorId
  }
}

resource autoShutdownPerfTestClusterAuthorization 'Microsoft.Authorization/roleAssignments@2021-04-01-preview' = {
  name: '${guid(perfTestClusterRoleAssignmentName)}'
  //  See https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/scope-extension-resources
  //  for scope for extension
  scope: perfTestCluster
  properties: {
    description: 'Give contributor on the cluster'
    principalId: autoShutdown.identity.principalId
    //  Fix the issue of the principal not being ready when deployment the assignment
    principalType: 'ServicePrincipal'
    roleDefinitionId: fullContributorId
  }
}

//  Authorize WF as reader on resource group
var readerId = 'acdd72a7-3385-48ef-bd42-f606fba81ae7'
var fullReaderId = '/subscriptions/${subscription().subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/${readerId}'
var rgRoleAssignmentName = '${resourceGroup().id}${autoShutdown.name}${fullReaderId}'

resource autoShutdownRgAuthorization 'Microsoft.Authorization/roleAssignments@2021-04-01-preview' = {
  name: '${guid(rgRoleAssignmentName)}'
  scope: resourceGroup()
  properties: {
    description: 'Give reader on the resource group'
    principalId: autoShutdown.identity.principalId
    //  Fix the issue of the principal not being ready when deployment the assignment
    principalType: 'ServicePrincipal'
    roleDefinitionId: fullReaderId
  }
}
