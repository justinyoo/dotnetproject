param location string
param kind string = 'CognitiveServices'
param environmentName string

var abbr = loadJsonContent('./../Extra/abbreviations.json')
var resourceName = toLower('${abbr.aiService}${kind}-abc9876-${environmentName}')

resource aiService_resource 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: resourceName
  location: location
  sku: {
    name: 'S0'
  }
  kind: kind
  identity: {
    type: 'None'
  }
  properties: {
    apiProperties: {
    }
    customSubDomainName: resourceName
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: 'Enabled'
  }
}

output Infra_AIService_Name string = aiService_resource.name