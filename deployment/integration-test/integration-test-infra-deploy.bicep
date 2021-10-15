/**************************************************/
//  Deploy an ADX cluster with multiple databases
//  ready to accomodate integration tests
//
//  A Logic App is deployed to shutdown the cluster
//  within 2-5 hours

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
var dbCountPerPrefix = 30
var shutdownWorkflowName = 'shutdownWorkflow'

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
    parent: cluster
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

resource autoShutdown 'Microsoft.Logic/workflows@2019-05-01' = {
    name: shutdownWorkflowName
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
                        interval: 1
                    }
                    evaluatedRecurrence: {
                        frequency: 'Hour'
                        interval: 1
                    }
                    type: 'Recurrence'
                }
            }
            actions: {
                'get-state': {
                    runAfter: {}
                    type: 'Http'
                    inputs: {
                        authentication: {
                            audience: environment().authentication.audiences[0]
                            type: 'ManagedServiceIdentity'
                        }
                        method: 'GET'
                        uri: '${environment().authentication.audiences[0]}subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Kusto/clusters/${cluster.name}?api-version=2021-01-01'
                    }
                }
                'if-running': {
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
                                uri: '${environment().authentication.audiences[0]}subscriptions/${subscription().subscriptionId}/resourceGroups/${resourceGroup().name}/providers/Microsoft.Kusto/clusters/${cluster.name}/stop?api-version=2021-01-01'
                            }
                        }
                        wait: {
                            runAfter: {}
                            type: 'Wait'
                            inputs: {
                                interval: {
                                    count: 2
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
                                    '@body(\'parse-payload\')?[\'properties\']?[\'state\']'
                                    'Running'
                                ]
                            }
                        ]
                    }
                    type: 'If'
                }
                'parse-payload': {
                    runAfter: {
                        'get-state': [
                            'Succeeded'
                        ]
                    }
                    type: 'ParseJson'
                    inputs: {
                        content: '@body(\'get-state\')'
                        schema: {
                            properties: {
                                properties: {
                                    properties: {
                                        state: {
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
            outputs: {}
        }
        parameters: {}
    }
}

var contributorId = 'b24988ac-6180-42a0-ab88-20f7382dd24c'
var fullRoleDefinitionId = '/subscriptions/${subscription().subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/${contributorId}'
var autoShutdownAssignmentInner = '${resourceGroup().id}${fullRoleDefinitionId}'
var autoShutdownAssignmentName = '${cluster.name}/Microsoft.Authorization/${guid(autoShutdownAssignmentInner)}'

resource autoShutdownAuthorization 'Microsoft.Kusto/clusters/providers/roleAssignments@2021-04-01-preview' = {
    name: autoShutdownAssignmentName
    properties: {
      description: 'Give contributor on the cluster'
      principalId:  autoShutdown.identity.principalId
      roleDefinitionId:  fullRoleDefinitionId
    }
  }

