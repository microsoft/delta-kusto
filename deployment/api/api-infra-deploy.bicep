param frontDoorName string

var UniqueId = uniqueString('${resourceGroup().id}kusto-x')
var Suffix = 'delta-kusto-${UniqueId}'
var AppInsights = 'app-monitor-${Suffix}'
var LogAnalytics = 'infra-monitor-${Suffix}'
// var Lake_Storage_Account_var = 'deltakustol${UniqueId}'
// var Blob_Storage_Account_var = 'deltakustob${UniqueId}'
// var API_Telemetry_Container = 'api-telemetry'
var AppPlan = 'app-plan-${Suffix}'
var WebApps = [
  {
    name: 'delta-kusto-api-staging-${Suffix}'
    env: 'staging'
  }
  {
    name: 'delta-kusto-api-prod-${Suffix}'
    env: 'prod'
  }
]
var DefaultFrontDoor = 'front-door-${Suffix}'
var FrontDoor = ((length(frontDoorName) == 0) ? DefaultFrontDoor : frontDoorName)

resource App_Insights 'Microsoft.Insights/components@2020-02-02' = {
  name: AppInsights
  location: resourceGroup().location
  tags: {}
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
  dependsOn: []
}

resource Log_Analytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: LogAnalytics
  location: resourceGroup().location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  dependsOn: []
}

// resource Lake_Storage_Account 'Microsoft.Storage/storageAccounts@2019-06-01' = {
//   name: Lake_Storage_Account_var
//   location: resourceGroup().location
//   sku: {
//     name: 'Standard_LRS'
//   }
//   kind: 'StorageV2'
//   properties: {
//     isHnsEnabled: true
//   }
// }

// resource Lake_Storage_Account_default_API_Telemetry_Container 'Microsoft.Storage/storageAccounts/blobServices/containers@2019-06-01' = {
//   name: '${Lake_Storage_Account_var}/default/${API_Telemetry_Container}'
//   properties: {
//     publicAccess: 'None'
//   }
//   dependsOn: [
//     Lake_Storage_Account
//   ]
// }

// resource Lake_Storage_Account_default 'Microsoft.Storage/storageAccounts/managementPolicies@2019-06-01' = {
//   parent: Lake_Storage_Account
//   name: 'default'
//   properties: {
//     policy: {
//       rules: [
//         {
//           name: 'api-telemetry-raw'
//           enabled: true
//           type: 'Lifecycle'
//           definition: {
//             filters: {
//               blobTypes: [
//                 'blockBlob'
//               ]
//               prefixMatch: [
//                 '${API_Telemetry_Container}/raw-telemetry'
//               ]
//             }
//             actions: {
//               baseBlob: {
//                 tierToCool: {
//                   daysAfterModificationGreaterThan: 1
//                 }
//               }
//             }
//           }
//         }
//       ]
//     }
//   }
// }

// resource Blob_Storage_Account 'Microsoft.Storage/storageAccounts@2019-06-01' = {
//   name: Blob_Storage_Account_var
//   location: resourceGroup().location
//   sku: {
//     name: 'Standard_LRS'
//   }
//   kind: 'StorageV2'
//   properties: {
//     isHnsEnabled: false
//   }
// }

// resource Blob_Storage_Account_default_API_Telemetry_Container 'Microsoft.Storage/storageAccounts/blobServices/containers@2019-06-01' = {
//   name: '${Blob_Storage_Account_var}/default/${API_Telemetry_Container}'
//   properties: {
//     publicAccess: 'None'
//   }
//   dependsOn: [
//     Blob_Storage_Account
//   ]
// }

resource App_Plan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: AppPlan
  location: resourceGroup().location
  sku: {
    name: 'B1'
    tier: 'Basic'
    size: 'B1'
    family: 'B'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
  dependsOn: []
}

resource WebApps_name 'Microsoft.Web/sites@2022-03-01' = [for item in WebApps: {
  name: item.name
  location: resourceGroup().location
  tags: {
    env: item.env
  }
  properties: {
    serverFarmId: App_Plan.id
    siteConfig: {
      alwaysOn: true
      linuxFxVersion: 'DOTNETCORE|6.0'
      appSettings: [
        // {
        //   name: 'storageConnectionString'
        //   value: 'DefaultEndpointsProtocol=https;AccountName=${Blob_Storage_Account_var};AccountKey=${listkeys(Blob_Storage_Account.id, '2019-06-01').keys[0].value};'
        // }
        // {
        //   name: 'telemetryContainerName'
        //   value: API_Telemetry_Container
        // }
        {
          name: 'env'
          value: item.env
        }
      ]
    }
  }
  dependsOn: [
    // Blob_Storage_Account
  ]
}]

resource Front_Door 'Microsoft.Network/frontDoors@2021-06-01' = {
  name: FrontDoor
  location: 'global'
  properties: {
    healthProbeSettings: [
      {
        name: 'healthProbeSettings'
        properties: {
          path: '/'
          protocol: 'Https'
          intervalInSeconds: 120
        }
      }
    ]
    loadBalancingSettings: [
      {
        name: 'loadBalancingSettings'
        properties: {
          sampleSize: 4
          successfulSamplesRequired: 2
        }
      }
    ]
    backendPools: [
      {
        name: 'prod-web-app'
        properties: {
          backends: [
            {
              address: reference(resourceId('Microsoft.Web/sites', WebApps[1].name), '2019-08-01').hostNames[0]
              backendHostHeader: reference(resourceId('Microsoft.Web/sites', WebApps[1].name), '2019-08-01').hostNames[0]
              httpsPort: 443
              httpPort: 80
              weight: 100
              priority: 1
            }
          ]
          loadBalancingSettings: {
            id: resourceId('Microsoft.Network/frontDoors/loadBalancingSettings', FrontDoor, 'loadBalancingSettings')
          }
          healthProbeSettings: {
            id: resourceId('Microsoft.Network/frontDoors/healthProbeSettings', FrontDoor, 'healthProbeSettings')
          }
        }
      }
    ]
    frontendEndpoints: [
      {
        name: 'defaultFrontendEndpoint'
        properties: {
          hostName: '${FrontDoor}.azurefd.net'
          sessionAffinityEnabledState: 'Disabled'
          sessionAffinityTtlSeconds: 0
        }
      }
    ]
    routingRules: [
      {
        name: 'prodRoutingRule'
        properties: {
          frontendEndpoints: [
            {
              id: resourceId('Microsoft.Network/frontDoors/frontendEndpoints', FrontDoor, 'defaultFrontendEndpoint')
            }
          ]
          acceptedProtocols: [
            'Https'
          ]
          patternsToMatch: [
            '/*'
          ]
          routeConfiguration: {
            '@odata.type': '#Microsoft.Azure.FrontDoor.Models.FrontdoorForwardingConfiguration'
            forwardingProtocol: 'HttpsOnly'
            backendPool: {
              id: resourceId('Microsoft.Network/frontDoors/backendPools', FrontDoor, 'prod-web-app')
            }
          }
          enabledState: 'Enabled'
        }
      }
    ]
    enabledState: 'Enabled'
  }
  dependsOn: [
    resourceId('Microsoft.Web/sites', WebApps[0].name)
    resourceId('Microsoft.Web/sites', WebApps[1].name)
  ]
}
