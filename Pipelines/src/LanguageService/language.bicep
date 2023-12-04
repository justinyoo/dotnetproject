param environmentName string
param location string

param sku string = 'F0'
param kind string= 'TextAnalytics'


var abbrs = loadJsonContent('./../Extra/abbreviations.json')
var resourceName = toLower('${abbrs.languageService}${kind}-abc987-${environmentName}')

resource languageService_resource 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: resourceName
  location: location
  sku: {
    name: sku
  }
  kind: kind
  identity: {
    type: 'SystemAssigned'
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

output Service_LanguageModel_Name string = languageService_resource.name
