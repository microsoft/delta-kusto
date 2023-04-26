param frontDoorName string
param location string = resourceGroup().location

var UniqueId = uniqueString('${resourceGroup().id}kusto-x')
var Suffix = 'delta-kusto-${UniqueId}'
// var AppInsights = 'app-monitor-${Suffix}'
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

// resource App_Insights 'Microsoft.Insights/components@2020-02-02' = {
//   name: AppInsights
//   location: location
//   tags: {}
//   kind: 'web'
//   properties: {
//     Application_Type: 'web'
//   }
//   dependsOn: []
// }

resource Log_Analytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: LogAnalytics
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
  dependsOn: []
}

resource App_Plan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: AppPlan
  location: location
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

resource WebAppsLoop 'Microsoft.Web/sites@2022-03-01' = [for item in WebApps: {
  name: item.name
  location: location
  tags: {
    env: item.env
  }
  properties: {
    serverFarmId: App_Plan.id
    siteConfig: {
      alwaysOn: true
      linuxFxVersion: 'DOTNETCORE|7.0'
      appSettings: [
        { //  This allows to run from a zip package
          name:  'WEBSITE_RUN_FROM_PACKAGE'
          value:  '1'
        }
        {
          name: 'env'
          value: item.env
        }
        // //  App Insights configuration ; taken from https://github.com/AndrewBrianHall/BasicImageGallery/blob/c55ada54519e13ce2559823c16ca4f97ddc5c7a4/CoreImageGallery/Deploy/CoreImageGalleryARM/azuredeploy.json#L238
        // {
        //   name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
        //   value: App_Insights.InstrumentationKey
        // }
        // {
        //   name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
        //   //  ~3 is for Linux, cf https://learn.microsoft.com/en-us/azure/azure-monitor/app/azure-web-apps-net-core?tabs=Linux%2Cwindows#application-settings-definitions
        //   value: '~3'
        // }
        // {
        //   name: 'XDT_MicrosoftApplicationInsights_Mode'
        //   value: 'recommended'
        // }
        // {
        //   name: 'APPINSIGHTS_PROFILERFEATURE_VERSION'
        //   value: '1.0.0'
        // }
        // {
        //   name: 'DiagnosticServices_EXTENSION_VERSION'
        //   value: '~3'
        // }
        // {
        //   name: 'APPINSIGHTS_SNAPSHOTFEATURE_VERSION'
        //   value: '1.0.0'
        // }
        // {
        //   name: 'SnapshotDebugger_EXTENSION_VERSION'
        //   value: '~1'
        // }
        // {
        //   name: 'XDT_MicrosoftApplicationInsights_BaseExtensions'
        //   value: 'disabled'
        // }
     ]
    }
  }
  dependsOn: [
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
    WebAppsLoop
  ]
}
