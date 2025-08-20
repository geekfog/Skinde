// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Azure BICEP ARM Template for Azure Resources (Generally)
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// PARAMETERS
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
@description('Azure Resource Group container')
@minLength(3)
param p_resourcegroup string // could this be default to something within resourceGroup() function?

@allowed([
  'PRD' // Production
  'DMO' // Demo
  'UAT' // User Acceptance Testing
  'STG' // Staging
  'QAS' // Quality Assurance
  'BLD' // Build
  'DV2' // Development 2
  'DEV' // Development
  'HFX' // Hotfix
  'INT' // Integration
  'SIT' // System Integration Testing
  'SBX' // Sandbox
  'PPE' // Pre-Production Environment
  'DRC' // Disaster Recovery
  'LAB' // Lab (Research, Prototyping, Experimentation)
  'SEC' // Security Testing
  'PRF' // Performance
  'TRN' // Training
])
@description('Environment')
@minLength(3)
@maxLength(3)
param p_deploy_environment string = 'DEV'

@description('Azure Entra Tenant ID')
@minLength(36)
param p_azure_tenant_id string

@description('<appname> used for Azure Resources prefix')
@minLength(4)
param p_appbasename string

@description('Azure Location internal naming reference (typically lowercase)')
@minLength(3)
param p_datacenter_reference string

@description('Azure Location for Resource Creation (typically lowercase)')
param p_datacenter_location string = resourceGroup().location

@description('Azure KeyVault Already Exists?')
param p_keyvault_exists bool = false

@description('Windows Time Zone')
@minLength(4)
param p_timezone_windows string

@description('Linux Time Zone')
@minLength(4)
param p_timezone_linux string

@description('Tags to attach to resources')
param p_commonTags object = {}

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// VARIABLES
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
var v_environment_upper = toUpper(p_deploy_environment)
var v_environment_lower = toLower(p_deploy_environment)
var v_appbasename_lower = toLower(p_appbasename)

var v_is_production = v_environment_lower == 'prd'

var v_appserviceplan_web_name = '${v_appbasename_lower}${v_environment_lower}${p_datacenter_reference}webasp'

// Note: Changing SKUs may not work for upgrading for specific resources.  Verify it will work or remove the resources and redeploy via template
var v_appserviceplan_web_sku = v_is_production ? 'B2' : 'B2' // B1 doesn't support custom apps, P0v3 is the lowest SKU that supports custom domains
var v_appserviceplan_web_cap = 1 // capacity

var v_appservice_web_name = '${v_appbasename_lower}${v_environment_lower}${p_datacenter_reference}web'

var v_keyvault_name = '${v_appbasename_lower}${v_environment_lower}${p_datacenter_reference}kv'
var v_keyvault_url = 'https://${v_keyvault_name}${environment().suffixes.keyvaultDns}'

var v_operationalinsights_name = '${v_appbasename_lower}${v_environment_lower}${p_datacenter_reference}oiw' // log analytics
var v_appinsights_name = '${v_appbasename_lower}${v_environment_lower}${p_datacenter_reference}ais'
var v_appinsights_sku = 'PerGB2018' // https://learn.microsoft.com/en-US/azure/azure-monitor/logs/resource-manager-workspace?tabs=bicep
var v_appinsights_retention_days = v_is_production ? 120 : 30
var v_appinsights_hearbeat_retention_days = v_is_production ? 30 : 7

var v_commonTags = union(p_commonTags, { Environment: p_deploy_environment })

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// RESOURCE GROUP
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
resource resource_resourceGroup 'Microsoft.Resources/tags@2020-06-01' = {
    name: 'default'
    scope: resourceGroup()
    properties: {
        tags: v_commonTags
    }
}

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// APPLICATION INSIGHTS (Shared)
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
resource resource_logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: v_operationalinsights_name
  location: p_datacenter_location
  tags: v_commonTags
  properties: {
      retentionInDays: v_appinsights_retention_days
      sku: {
          name: v_appinsights_sku
      }
  }
}

resource resource_logAnalyticsWorkspace_table 'Microsoft.OperationalInsights/workspaces/tables@2021-12-01-preview' = {
  parent: resource_logAnalyticsWorkspace
  name: 'Heartbeat'
  properties: {
    retentionInDays: v_appinsights_hearbeat_retention_days
  }
}

resource resource_appinsights 'Microsoft.Insights/components@2020-02-02' = {
  name: v_appinsights_name
  location: p_datacenter_location
  tags: v_commonTags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Redfield'
    Request_Source: 'IbizaWebAppExtensionCreate'
    RetentionInDays: 90
    WorkspaceResourceId: resource_logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// AZURE APP SERVICE: Website (Web App)
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
resource resource_appserviceplan_web 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: v_appserviceplan_web_name
  location: p_datacenter_location
  tags: v_commonTags
  sku: {
    name: v_appserviceplan_web_sku
    capacity: v_appserviceplan_web_cap
  }
  kind: 'linux' // app = Windows-based, linux = Linux-based
  properties: {
    reserved: true // for Linux
  }
}

resource resource_appservice_web 'Microsoft.Web/sites@2022-09-01' = {
  name: v_appservice_web_name
  location: p_datacenter_location
  tags: v_commonTags
  kind: 'app' // app = Web App, functionapp = Function App
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    hostNameSslStates: [
      {
        name: '${v_appservice_web_name}.azurewebsites.net'
        sslState: 'Disabled'
        hostType: 'Standard'
      }
    ] 
    serverFarmId: resource_appserviceplan_web.id
    httpsOnly: true // for HTTP
    siteConfig: { 
      //netFrameworkVersion: 'v8.0' // for Windows
      linuxFxVersion: 'DOTNETCORE|8.0' // for Linux
      alwaysOn: true
      minTlsVersion: '1.2'
      websiteTimeZone: p_timezone_windows
      ftpsState: 'Disabled' // Disabled: FTPS/FTP turned off, FtpsOnly: Only FTPS allowed and FTP is disabled
      remoteDebuggingEnabled: false //!v_is_production
      remoteDebuggingVersion: 'VS2022'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: p_deploy_environment
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: resource_appinsights.properties.InstrumentationKey
        }
        {
          name: 'TZ'
          value: p_timezone_linux
        }
        {
          name: 'KeyVaultEndpoint'
          value: v_keyvault_url
        }
      ]
      metadata: [
        {
          name: 'CURRENT_STACK'
          value: 'dotnet'
        }
      ]
    }
    reserved: true // for Linux
  }
}

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// KEYVAULT
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
module module_azurekeyvault 'azure-arm-akv.bicep' = {
  name: 'azure-keyvault-deployment'
  scope: resourceGroup(p_resourcegroup)
  params: {
    p_keyvault_create: !p_keyvault_exists
    p_azure_tenant_id: p_azure_tenant_id
    p_datacenter_location: p_datacenter_location
    p_keyvault_name: v_keyvault_name
    p_appservice_access: [
      resource_appservice_web.identity.principalId
      //resource_appservice_api.identity.principalId
    ]
    p_commonTags: v_commonTags
  }
}

// ~End~