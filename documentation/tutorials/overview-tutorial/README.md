# Delta Kusto Overview Tutorial

In this tutorial, we are going to use the Delta Kusto CLI and tour its functionalities.

We are going to cover a few scenarios.  Because of this breath we won't cover details such as how to download Delta Kusto or authentication to ADX Clusters.  We suggest looking at [other tutorials](../README.md) for more details.  Specifically, [Delta Kusto Download / Install](../install/README.md) and [Authenticating to ADX Clusters](../authentication/README.md) would be useful.

This tutorial is also available in video format:

<iframe width="560" height="315" src="https://www.youtube.com/embed/2neGBKlcoOA" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

## Delta Kusto

As explained in the [main documentation](https://github.com/microsoft/delta-kusto):

>>> Delta-Kusto is a Command-line interface (CLI) enabling Continuous Integration / Continuous Deployment (CI / CD) automation with Kusto objects (e.g. tables, functions, policies, security roles, etc.) in Azure Data Explorer (ADX) databases. It can work on a single database, multiple databases, or an entire cluster. It also supports multi-tenant scenarios.

Delta Kusto is a CLI.  It comes as a single-file executable availabe on both Windows and Linux.

The CLI accepts the path to a [parameter file](../../parameter-file.md).  The parameter file is a YAML file instructing Delta Kusto on what job to perform.  The parameter file has the following schema:

```yaml
schema:  "string"
sendErrorOptIn:  "boolean"
failIfDrops:  "boolean"
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
```

So a single call to Delta Kusto can run multiple jobs.  This feature can enable change management on multi-tenant solution within Azure Data Explorer.  In this tutorial we'll only run one job at the time.

A job consist of a current source, a target source and a set of actions.  Sources can either be a script (or set of scripts) or an ADX Database.  So Delta Kusto can compute the delta between 2 ADX Databases, between a script and an ADX Database or between 2 scripts (offline mode).  An action specify where to push the delta script:  on the console, on files or on the current source (if the current source is an ADX Database).

This simple configuration enables multiple scenarios as we'll see in this tutorial.

The `tokenProvider` part is explained with more details in the [Authenticating to ADX Clusters tutorial](../authentication/README.md).

The parameter file is meant to be persisted with scripts in source control.  For that reason we do not want to put sensitive information such as credentials.  We can override parameter file values on the common line for that reason.  Overriding value can also be useful to re-use the same parameter file in different environments (e.g. by overriding the cluster URIs / database name).

In this tutorial, we are going to use only one cluster but Delta Kusto can compute delta between multiple clusters.

## Brownfield Dev

[dev-start-samples.kql](dev-start-samples.kql)

[download-dev.yaml](download-dev.yaml)

```
delta-kusto -p download-dev.yaml 
```

```
cat dev-state.kql
```

##  Push to prod

[push-to-prod.yaml](push-to-prod.yaml)

```
delta-kusto -p push-to-prod.yaml
```

Look at prod

##  Dev some more

[modify-dev.kql](modify-dev.kql)

Trigger the 'fail to drop'

[push-to-prod.yaml](push-to-prod.yaml)

##  Bring back prod:  ADX Database (current) to ADX Database (target)

Bring prod back to dev

[prod-to-dev.yaml](prod-to-dev.yaml)

## Controlled environment:   Kusto scripts (current) to Kusto scripts (target)

[modify-dev.kql](modify-dev.kql)

cp dev-start-samples.kql prod-state.kql

compare with dev-state

[offline-delta.yaml](offline-delta.yaml)

