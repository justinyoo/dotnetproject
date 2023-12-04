param environmentName string
param location string

param name string = 'abc123plan'

var abbrs = loadJsonContent('./../Extra/abbreviations.json')
var resourceName = toLower('${abbrs.appServicePlan}${name}-${environmentName}')

resource appServicePlan_resource 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: resourceName
  location: location
  sku: {
    name: 'F1'
    tier: 'Free'
    size: 'F1'
    family: 'F'
    capacity: 0
  }
  kind: 'app'
  properties: {
    perSiteScaling: false
    elasticScaleEnabled: false
    maximumElasticWorkerCount: 1
    isSpot: false
    reserved: false
    isXenon: false
    hyperV: false
    targetWorkerCount: 0
    targetWorkerSizeId: 0
    zoneRedundant: false
  }
}

output Infra_AppServicePlan_Id string= appServicePlan_resource.id
output Infra_AppServicePlan_Name string= appServicePlan_resource.name