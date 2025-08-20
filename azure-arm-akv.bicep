// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Azure BICEP Azure KeyVault ARM Template
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
@description('Whether to create the Azure KeyVault')
param p_keyvault_create bool

@description('Azure (Entra ID) Tenant ID (GUID)')
@minLength(36)
param p_azure_tenant_id string

@description('Azure Data Center Location')
@minLength(1)
param p_datacenter_location string

@description('Azure KeyVault Name')
@minLength(3)
param p_keyvault_name string

@description('Array of principalIds (objectIds) to grant Key Vault access')
param p_appservice_access array = []

@description('Tags to attach to resources')
param p_commonTags object = {}

var v_azdoconnection_access = [
  '282bba1c-7c19-4c6d-90a0-6b2e576d0760' // "AzDO PTSNorth-MS Azure CSP" Azure APP REGISTRATION Object ID created from Azure DevOps "PTSNorth - Microsoft Azure CSP" service connection
  'eb2c2ca9-ab46-46d3-a79d-a9f8ab64b318' // "AzDO PTSNorth-MS Azure CSP" Azure ENTERPRISE APPLICATION Object ID created from Azure DevOps "PTSNorth - Microsoft Azure CSP" service connection
]

var v_azdoconnection_permissions = {
  secrets: [
    'Get'
    'List'
  ]
}

var v_azdoconnection_policies = [for l_azdoconnection_access in v_azdoconnection_access: { 
      tenantId: p_azure_tenant_id
      objectId: l_azdoconnection_access
      permissions: v_azdoconnection_permissions
    }
  ]

var v_group_access = [
  '69534a02-77c8-4135-9549-a5d55e184b88' // BITS Team DEV
  '6b6a19af-e0a4-4e93-9d96-1fdbf86099bf' // BITS Team QA
]

var v_group_permissions = {
    secrets: [
      'Get'
      'List'
      'Set'
      'Delete'
      'Recover'
      'Backup'
      'Restore'
    ]
  }

var v_group_policies = [for l_group_access in v_group_access: {
  tenantId: p_azure_tenant_id
  objectId: l_group_access
  permissions: v_group_permissions
}]

var v_appservice_permissions = {
  secrets: [
    'Get'
    'List'
  ]
}

var v_appservice_policies = [for l_appservice_access in p_appservice_access: {
    tenantId: p_azure_tenant_id
    objectId: l_appservice_access
    permissions: v_appservice_permissions
  }
]

resource resource_keyvault 'Microsoft.KeyVault/vaults@2023-02-01' = if (p_keyvault_create) {
  name: p_keyvault_name
  location: p_datacenter_location
  tags: p_commonTags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: p_azure_tenant_id
    accessPolicies: concat(
      v_azdoconnection_policies,
      v_appservice_policies,
      v_group_policies
    )
  }
}