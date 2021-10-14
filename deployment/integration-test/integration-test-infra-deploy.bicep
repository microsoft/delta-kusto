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
var 

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
    name: 'string'
    location: 'string'
    tags: {}
    properties: {
        definition: {
            "$schema": "https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#",
            "contentVersion": "1.0.0.0",
            "parameters": {},
            "triggers": {
                "Recurrence": {
                    "recurrence": {
                        "frequency": "Hour",
                        "interval": 1
                    },
                    "evaluatedRecurrence": {
                        "frequency": "Hour",
                        "interval": 1
                    },
                    "type": "Recurrence"
                }
            },
            "actions": {
                "get-state": {
                    "runAfter": {},
                    "type": "Http",
                    "inputs": {
                        "authentication": {
                            "audience": "https://management.core.windows.net/",
                            "type": "ManagedServiceIdentity"
                        },
                        "method": "GET",
                        "uri": "https://management.azure.com/subscriptions/867feb0a-8313-464d-ad48-b4904383f9bc/resourceGroups/delta-kusto/providers/Microsoft.Kusto/clusters/clusteryenycav4i2vma?api-version=2021-01-01"
                    }
                },
                "if-running": {
                    "actions": {
                        "stop-cluster": {
                            "runAfter": {
                                "wait": [
                                    "Succeeded"
                                ]
                            },
                            "type": "Http",
                            "inputs": {
                                "authentication": {
                                    "audience": "https://management.core.windows.net/",
                                    "type": "ManagedServiceIdentity"
                                },
                                "method": "POST",
                                "uri": "https://management.azure.com/subscriptions/867feb0a-8313-464d-ad48-b4904383f9bc/resourceGroups/delta-kusto/providers/Microsoft.Kusto/clusters/clusteryenycav4i2vma/stop?api-version=2021-01-01"
                            }
                        },
                        "wait": {
                            "runAfter": {},
                            "type": "Wait",
                            "inputs": {
                                "interval": {
                                    "count": 2,
                                    "unit": "Hour"
                                }
                            }
                        }
                    },
                    "runAfter": {
                        "parse-payload": [
                            "Succeeded"
                        ]
                    },
                    "expression": {
                        "and": [
                            {
                                "equals": [
                                    "@body('parse-payload')?['properties']?['state']",
                                    "Running"
                                ]
                            }
                        ]
                    },
                    "type": "If"
                },
                "parse-payload": {
                    "runAfter": {
                        "get-state": [
                            "Succeeded"
                        ]
                    },
                    "type": "ParseJson",
                    "inputs": {
                        "content": "@body('get-state')",
                        "schema": {
                            "properties": {
                                "properties": {
                                    "properties": {
                                        "state": {
                                            "type": "string"
                                        }
                                    },
                                    "type": "object"
                                }
                            },
                            "type": "object"
                        }
                    }
                }
            },
            "outputs": {}
        }
      endpointsConfiguration: {
        connector: {
          accessEndpointIpAddresses: [
            {
              address: 'string'
            }
          ]
          outgoingIpAddresses: [
            {
              address: 'string'
            }
          ]
        }
        workflow: {
          accessEndpointIpAddresses: [
            {
              address: 'string'
            }
          ]
          outgoingIpAddresses: [
            {
              address: 'string'
            }
          ]
        }
      }
      integrationAccount: {
        id: 'string'
      }
      integrationServiceEnvironment: {
        id: 'string'
      }
      parameters: {}
      state: 'string'
    }
  }
