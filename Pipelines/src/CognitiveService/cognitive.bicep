param environmentName string
param location string


param kind string = 'SpeechServices'
param sku string = 'F0'

var abbrs = loadJsonContent('./../Extra/abbreviations.json')
var resourceName = toLower('${abbrs.cognitiveService}${kind}-${environmentName}')


resource cognitiveService_resource 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: resourceName
  location: location
  sku:{
	name: sku
  }
  kind: kind
  identity: {
    type: 'None'
  }
  properties: {
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    publicNetworkAccess: 'Enabled'
  }
}

output Service_CognitiveService_Name string = cognitiveService_resource.name



