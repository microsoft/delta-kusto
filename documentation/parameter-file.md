#   Parameter file

Delta Kusto requires a parameter file...

Both JSON & YAML are supported.  Here and everywhere in the documentation, we use YAML.

Examples?

## Format

```yaml
schema:  "string"
sendErrorOptIn:  "boolean"
failIfDrops:  "boolean"
jobs: 
  myJob:
    priority:  "integer"
    current:
        database:
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
        pushToConsole:  "boolean"
        pushToCurrentCluster:  "boolean"
tokenProvider:
    tokenMap:
        myToken:
            clusterUri:  "string"
            token:  "string"
    login:
        tenantId:  "string"
        clientId:  "string"
        secret:  "string"
```

## Property Values

The following tables describe the values you need to set in the schema.

###  Root / Main object

Name|Type|Required|Default|Value
-|-|-|-|-
schema|string|No|N/A|Used to identify the schema of the parameters.  This is for future use and is currently ignored.
sendErrorOptIn|boolean|No|false|Sends error telemetry to a centralized service for pro-active troubleshooting by Delta-Kusto development team (telemetry isn't public).  Opting in basically allows pro-active improvements on Delta-Kusto.  See [telemetry](telemetry.md) for more details.
failIfDrops|boolean|No|false|Setting this to `true` will force Delta Kusto to fail if drop commmands are detected in deltas.  See [failIfDrops flag](failIfDrops.md) for details.
jobs|dictionary|Yes|N/A|Dictionary mapping a job *name* to a [Job object](#job-object).
tokenProvider|object|No|N/A|[Token Provider Object](#token-provider-object).

The reason the `jobs` are in a dictionary is to faciliate the overriding of values (see [Parameter Overrides](parameter-overrides.md) for details).

### Job object

### Token Provider object
