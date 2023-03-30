#   Parameter file

Delta Kusto requires a parameter file...

Both JSON & YAML are supported.  Here and everywhere in the documentation, we use YAML.

Examples?

## Format

```yaml
schema:  "string"
sendErrorOptIn:  "boolean"
failIfDataLoss:  "boolean"
jobs: 
  myJob:
    priority:  "integer"
    current:
        adx:
            clusterUri:  "string"
            database:  "string"
        scripts:
            -   filePath:  "string"
                folderPath:  "string"
                extensions:
                - "string"
    target:  "same as current"
    action:
        filePath:  "string"
        folderPath:  "string"
        csvPath:  "string"
        usePluralForms:  "boolean"
        pushToConsole:  "boolean"
        pushToCurrent:  "boolean"
tokenProvider:
    tokens:
        myToken:
            clusterUri:  "string"
            token:  "string"
    login:
        tenantId:  "string"
        clientId:  "string"
        secret:  "string"
    systemManagedIdentity:  "boolean"
    userManagedIdentity:
        clientId:  "string"
    userPrompt:
        tenantId:  "string"
        userId:  "string"
    azCli:
        interactive:  "boolean"
    noAuth:  "boolean"
```

## Property Values

The following tables describe the values you need to set in the schema.

###  Root / Main object

Name|Type|Required|Default|Value
-|-|-|-|-
schema|string|No|N/A|Used to identify the schema of the parameters.  This is for future use and is currently ignored.
sendErrorOptIn|boolean|No|false|Sends error telemetry to a centralized service for pro-active troubleshooting by Delta-Kusto development team (telemetry isn't public).  Opting in basically allows pro-active improvements on Delta-Kusto.  See [telemetry](telemetry.md) for more details.
failIfDataLoss|boolean|No|false|Setting this to `true` will force Delta Kusto to fail if commmands resulting in potential data loss are detected in deltas.  See [failIfDataLoss flag](failIfDataLoss.md) for details.
jobs|dictionary|Yes|N/A|Dictionary mapping a job *name* to a [Job object](#job-object).
tokenProvider|object|No|N/A|[Token Provider Object](#token-provider-object).

The reason the `jobs` are in a dictionary is to faciliate the overriding of values (see [Parameter Overrides](parameter-overrides.md) for details).

### Job object

Name|Type|Required|Default|Value
-|-|-|-|-
priority|integer|No|Infinity|Number to determine the priority of the job.  Lowest priority runs first.  By default, jobs could run in any order.
current|object|No|Empty source|Current state, [Source object](#source-object).  If not specified, an empty database is assumed.
target|object|Yes|N/A|Destination state, [Source object](#source-object).
action|object|Yes|N/A|Actions to do with the delta.  [Action object](#action-object).

### Token Provider object

Name|Type|Required|Default|Value
-|-|-|-|-
tokens|dictionary|No|N/A|Dictionary mapping a token *name* to a [Token object](#token-object).
login|object|No|N/A|[Login object](#login-object).
systemManagedIdentity|boolean|No|false|Opting in means using the system managed identity of whatever compute Delta Kusto runs on (e.g. Azure VM, AKS, etc.).
userManagedIdentity|object|No|N/A|[User Managed Identity object](#user-managed-identity-object).
userPrompt|object|No|N/A|[User Prompt object](#user-prompt-object).
azCli|object|No|N/A|[Az CLI object](#az-CLI-object).
noAuth|boolean|No|false|Opting in means no authentication will be performed.  This is useful when using [Kusto Emulator](https://learn.microsoft.com/en-us/azure/data-explorer/kusto-emulator-overview)

Although none of the properties are not required, one and only one of them must be provided if the token provider object is provided.

### Source object

This object is used to configure both `current` and `target` property in a job.

Name|Type|Required|Default|Value
-|-|-|-|-
adx|object|No|N/A|Configure the source as an Azure Data Explorer (ADX) database.  [ADX source object](#adx-source-object).
scripts|object|No|N/A|Configure the source as one or many KQL scripts.  [Scripts source object](#scripts-source-object).

### ADX source object

Name|Type|Required|Default|Value
-|-|-|-|-
clusterUri|string|Yes|N/A|Cluster URI of the ADX Cluster.
database|string|Yes|N/A|Database name

### Scripts source object

Name|Type|Required|Default|Value
-|-|-|-|-
filePath|string|No|N/A|Target a specific script file.
folderPath|string|No|N/A|Target an entire folder (and sub folders)
extensions|string collection|No|N/A|Filters the files present in the folder (and sub folders) specified by `folderPath` by extensions.

Although all properties are not required, only the following combinations are possible:

* `filePath` alone
* `folderPath` alone
* `folderPath` with `extensions`

### Action object

Name|Type|Required|Default|Value
-|-|-|-|-
filePath|string|No|N/A|Specify a file path to export all Kusto delta commands
folderPath|string|No|N/A|Specify a folder path to export all Kusto delta commands.  Commands will be pushed in a folder structure by type (e.g. all functions will be under a *functions* folder)
csvPath|string|No|N/A|Specify a file path to export all Kusto delta commands in CSV format for easier filtering / post-processing.
usePluralForms|boolean|No|false|If `true`, bundle commands together into plural forms for file outputs (single or multiple).  Plural forms are always used when commands are pushed to an ADX cluster.
pushToConsole|boolean|No|false|If `true`, the delta commands are *printed* on the console during execution.
pushToCurrent|boolean|No|false|If `true`, the commands are executed on the *current* database.  For this to work, the current source of the [Job object](#job-object) must be an [ADX source object](#adx-source-object).

Although all properties are not required, at least one must be non-empty or true.  At most one of `filePath`, `folderPath` and `csvPath` can be specified.

### Token object

Name|Type|Required|Default|Value
-|-|-|-|-
clusterUri|string|Yes|N/A|Cluster URI where to use this token
token|string|Yes|N/A|Value of the bearer token

Token object is used in order not to have to provide a principal secret.  Authentication can be done outside Delta Kusto with only the produced token passed to it.

Token should be bearer token for the resource `clusterUri` (e.g. retrieved using the [AAD REST API](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/api/rest/request#examples)).

### Login object

Name|Type|Required|Default|Value
-|-|-|-|-
tenantId|string|Yes|N/A|Azure AD Tenant ID
clientId|string|Yes|N/A|Client ID, also known as *Application ID*, of a Azure AD Service Principal
secret|string|Yes|N/A|Secret associated to an Azure AD Service Principal

### User Managed Identity object

Name|Type|Required|Default|Value
-|-|-|-|-
clientId|string|Yes|N/A|Client ID of the User Managed Identity

### User Prompt object

Name|Type|Required|Default|Value
-|-|-|-|-
tenantId|string|No|N/A|Azure AD Tenant ID
userId|string|No|N/A|User ID

### Az CLI object

Name|Type|Required|Default|Value
-|-|-|-|-
interactive|boolean|No|false|Is the Az CLI operates in *interactive* mode or not
