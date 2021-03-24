#   Parameter file

Delta Kusto requires a parameter file...

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

###  Item 1