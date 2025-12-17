# Introduction
<img src="Images/favicon-0128.png" alt="favicon" style="zoom:80%; float:right;" />Skinde (Sub-Kinde Administrator) is an open source tool built on top of the Kinde OAuth2 SaaS platform. It offers a lightweight subset of Kinde's capabilities focused on secured delegation of user and organization management.

Key Features:

- **Delegation of User and Organization Management**: Enables admins and authorized users to delegate creation, modification, and management of users and organization,.
- **OAuth2-Based Authentication**: Utilizes the OAuth2 and OpenID Connect standards for secure authentication and robust token handling.
- **Role Management**: Supports retrieving and assignment of roles for users.
- **Subset of Kinde's Capabilities**: Provides just enough features for basic user and organization management  without access (or overhead) of the full SaaS platform (which also include application management). Delegated administration isn't currently available within the Kinde SaaS platform.
- **Open Source**: Freely available for contribution, integration, and self-hosting. 

Kinde leverages [Well-Known](https://localhost:7076/.well-known/openid-configuration) endpoint via the established OAuth2 standard.

**Why is this being done?** To demonstrate technology capabilities, give back to the community, and take advantage of collaboration.

# Licensing

This uses the GNU Public License v3 (GPL 3.0) [license](./LICENSE.txt).

# Potential Future Enhancements

These are expected, but not guaranteed, enhancements in the future.

- Improved documentation to reflect the steps required to become operational
- Add User Activity Listing, per user and globally
- Preserve past filtering when returning (such as filtering specific users in the user list, editing a user, and returning to the user list)
- GitHub Actions (GitHub Pipeline)

# Getting Started

*This is under extreme development*

1. Review the [technologies](#technologies)
2. Configure Kinde
   - To allow a user to have access to the application, make sure they are part of the role *admin* (default key value) or the overriden role specified by environmental variable *AdminAccessRoleKey*.
3. (Optional) Create Pipelines: Azure DevOps
   - Leverage [azure-pipelines.yaml](./azure-pipelines.yaml)
   - Update the following variables
     - Time Zone Windows: Default "Central Standard Time"
     - Time Zone Linux: Default "America/Chicago"
     - App Service Plan (e.g., B2)
4. (Optional)
   - Leverage GitHub Actions version


# Technologies

## Development

- [Visual Studio 2022](https://visualstudio.microsoft.com/) with [Bundler & Minifier 2022+](https://marketplace.visualstudio.com/items?itemName=Failwyn.BundlerMinifier64)
  - Visual Studio 2026 also works (initial testing with Insider edition with Bundler & Minifier caused issues)

- [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/?view=aspnetcore-8.0)
- [C#](https://dotnet.microsoft.com/en-us/languages/csharp)
- [MudBlazor](https://www.mudblazor.com/)
- [Git for Windows](https://gitforwindows.org/) (ARM64 or x64) -- used for command-line git support with *Skinde.Build*

Since *Skinde.Ui* uses, and by default [MSBUILD locks](https://github.com/dotnet/msbuild/issues/4743), the DLL built by *Skinde.Build*, set the following environmental variable to prevent the locking:

```bat
SET MSBUILDDISABLENODEREUSE=1
```

This should be set prior to opening Visual Studio, such as via the Control Panel's System Properties Environmental Variables.

## Azure Pipelines

Azure Pipelines leverages [YAML](https://learn.microsoft.com/en-us/azure/devops/pipelines/yaml-schema/?view=azure-pipelines) (for CI/CD) and [BICEP](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/overview?tabs=bicep) ([Azure Resource Deployments](#azure-resource-deployments)). You can fork the repo and create your own YAML files or use the existing and leverage variable group **Skinde-Parameters** in the pipeline to specify the parameters required to [azure-pipelines.yaml](./azure-pipelines.yaml).

## Azure Resource Deployments

The first time a deployment is run for [Azure Cloud Services](https://portal.azure.com), there will be errors (which don't stop the process) because Bicep needs to be run to create Azure KeyVault resources. But this is done after Azure Pipelines references the KeyVault so it is available for the various tasks. Re-running the Pipeline multiple times overcomes the issues as resources and security assignments are completed.

Once the KeyVault has been created, it can be used to configure appropriate settings.

# Application Settings

## Settings Overview

Settings locations (highest priority at the top):

1. **Secrets.json**: Copy on developer machine when doing local debugging, stored (on Windows) in *%APPDATA%\Microsoft\UserSecrets\Skinde*.

2. **Azure KeyVault**: Secured access  by the application, pulled in during startup. Familiarity with with Azure KeyVault access via App Services is required and outside the scope of this document. For hierarchical settings, since the colon (:) symbol is not allowed, use double-dash (--) in its place. For example:

   ```
   {
     "toplevel": {
       "nextlevel": "nextvalue"
     }
   }
   ```

   Translating this JSON to Azure KeyVault would have the following Secret assignment: 
   	Name: *toplevel--nextlevel*
   	Current Version: *nextvalue*

3. **Environmental Variables**: This can be within the Environment settings of an Azure App Service, for example, or Operating System environment variables (e.g., via the SET command).

4. **appsettings.\{ENV}.json**: environment-specific settings. **Should be limited usage because it is checked into source control**.

5. **appsettings.json**: used for all environments, unless overridden

## Configuring Settings

The following settings configure the Azure Web App (App Services):

```json
{
  "KeyVaultEndpoint": "https://<KEYVAULT-NAME>.vault.azure.net", // Optional, if using, recommend setting Environmental Variable in Azure Web App
  "MaxOrganizationAssignments": 0, // Optional, 0=unlimited (or unspecified), #=maximum number of organizations allowed per user
  "ExcludeRoles": "<comma-separated list of roles based on their KEY value>", // Optional, uses KEY value, which is enforced as unique and independent per Kinde Environment unlike ID
  "DisplaySettingSource": false, // Whether to display the source of the settings (best via Environment Variable or secrets.json)
  "AdminAccessRoleKey":"<KINDE-ROLE-TO-ACCESS-APP>", // Optional, will use admin role by default if not specified

  "OAuth2": {
    "AuthUrl": "<AUTH-BASE-URL>", // Auth URL generated by Kinde Back-End Application
    "ClientId": "<CLIENT ID>", // Client ID generated by Kinde Back-End Application
    "ClientSecret": "<CLIENT SECRET>", // Client Secret generated by Kinde Back-End Application
    "Scopes": "<SCOPES>", // default setting stored in appsettings.json
    "CallbackPath": "/signin-oidc" // default setting stored in appsettings.json
  },

  "KindeApi": {
    "BaseEndpoint": "https://<YOUR_M2M-BASED_KINDE_SUBDOMAIN.kinde.com", // Configured in Kinde M2M Application
    "ClientId": "<M2M-BASED CLIENT ID>", // Client ID generated in Kinde
    "ClientSecret": "<M2M-BASED CLIENT SECRET>" // Client Secret generated in Kinde
  }
}
```

Additional Notes

- The ARM Template automatically leverages the BICEP ARM Template and sets the value of *KeyVaultEndpoint*.
- It is expected the **Client ID** and **Secret** be stored within Azure KeyVault or secrets.json (copy on developer machine when doing local debugging) so not in source control. 
- M2M is a specific machine-to-machine client, used for **Kinde Management API** access (separate from the OAuth2 client configured in Kinde). 

The M2M Application must be approved to access the **Kinde Management API** with the following *19 scopes* enabled:

```
create:organizations
create:organization_user_roles
create:organization_users
create:user_identities
create:users

delete:identities
delete:organization_user_roles
delete:organization_users
delete:user_mfa

read:organizations
read:organization_user_roles
read:organization_users
read:roles
read:user_identities
read:user_mfa
read:users

update:organizations
update:organization_users
update:users
```

# Development Notes

## JSON Library

Newtonsoft.Json is used, rather than System.Text.Json, because of the need to filter properties that are sent to the Kinde API Management endpoints. If undesired data is sent, the Kinde APIs return with a 404 error and the call fails. They only allow submission of data that is supported. In order to avoid creating separate classes for the various API calls, the following pattern is leveraged:

```c#
[JsonProperty("kinde_property")]
private string DesiredPropertySetter { set => DesiredProperty = value; }
[JsonIgnore]
public string DesiredProperty { set; get; }
```

This allows the data to be read when hydrated with JSON deserialization, but won't be included in the JSON serialization process. This example is with a string, but could be any data type.

## Security

Skinde is both a client (back-end) for users to authenticate against Kinde and a client (M2M) to the Kinde API Management. This requires separate access tokens:

1. The authenticated user accessing Skinde, which isn't used for any API calls and cannot call the Kinde API Management endpoints) 

2. The client_credentials-based access token to make calls, based on the assigned scopes (which are not included in the request of the access token) within the Kinde Admin Management for the M2M client.

## Version Updates

Automated updating of [Directory.Build.Props](./Directory.Build.Props) occurs during Visual Studio building (so as to avoid issues during the Azure DevOps Pipeline Build process) based on the current release branch, independent of the Build Configuration (environment). This is managed via the project [Skinde.Build](./Skinde.Build/Skinde.Build.csproj) (which updates the file) and [Skinde.Ui](./Skinde.Ui/Skinde.Ui.csproj) (which initiates the update).

Right now, this requires the use of a release branch when doing development. Additional considerations will be required if feature branches are leveraged with merging into release branches.

## Projects

| Project        | Purpose                                                      | .NET Technology   |
| -------------- | ------------------------------------------------------------ | ----------------- |
| Skinde.Build   | Update *Directory.Build.Props* programmatically as a Task    | .NET Standard 2.0 |
| Skinde.Client  | API calls into Kinde (it is acting as an M2M Client)         | .NET 8            |
| Skinde.Tests   | Unit Testing (Not currently in use other than to have as part of the pipeline process) | .NET 8            |
| Skinde.Ui      | Main Application: Website                                    | .NET 8            |
| Skinde.Utility | Independent Helpers                                          | .NET 8            |

# Resources

- [Kinde API Documentation](https://docs.kinde.com/kinde-apis/)
- [Kinde Access Token for API Documentation](https://docs.kinde.com/developer-tools/kinde-api/access-token-for-api/)
- [Kinde API Rate Limits](https://docs.kinde.com/developer-tools/kinde-api/api-rate-limits/)
- [Bootstrap Icons](https://icons.getbootstrap.com/?q=building) - converting SVG to CSS background-image:
  1. Replace *currentColor* with *white*
  2. Replace " with '
  3. Replace < with %3C
  4. Replace \> with %3E
  5. Remove all new lines
- [MudBlazor Components](https://www.mudblazor.com/components/menu#simple-menu)

# Version History

| Date       | Version  | .NET Platform | Notes                                                        |
| ---------- | -------- | ------------- | ------------------------------------------------------------ |
| 2025-12-16 | 01.08.02 | .NET 8        | Upgrade NuGet Packages (security fixes of vulnerabilities)   |
| 2025-08-19 | 01.08.01 | .NET 8        | Conversion from closed-source on Azure DevOps to open-source on GitHub. Add support for configuration-based Azure Resource naming with pipeline variables. |

# Contributions

- Hans Dickel [✉️](mailto:hans@geekfog.net) [🌍](https://www.linkedin.com/in/hansdickel) [💻](https://github.com/geekfog)

\~ END \~ 