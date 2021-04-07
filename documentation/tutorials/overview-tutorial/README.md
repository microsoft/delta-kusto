# Delta Kusto Overview Tutorial

In this tutorial, we are going to use the Delta Kusto CLI and tour its functionalities.

We are going to cover a few scenarios.  Because of this breath we won't cover details such as how to download Delta Kusto or authentication to ADX Clusters.  We suggest looking at [other tutorials](../README.md) for more details.  Specifically, [Delta Kusto Download / Install](../install/README.md) and [Authenticating to ADX Clusters](../authentication/README.md) would be useful.

This tutorial is also available in video format:

<iframe width="560" height="315" src="https://www.youtube.com/embed/2neGBKlcoOA" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

## Delta Kusto

As explained in the [main documentation](https://github.com/microsoft/delta-kusto):

>>> Delta-Kusto is a Command-line interface (CLI) enabling Continuous Integration / Continuous Deployment (CI / CD) automation with Kusto objects (e.g. tables, functions, policies, security roles, etc.) in Azure Data Explorer (ADX) databases. It can work on a single database, multiple databases, or an entire cluster. It also supports multi-tenant scenarios.

Delta Kusto comes as a single-file executable availabe on both Windows and Linux distributed as a [GitHub release](https://github.com/microsoft/delta-kusto/releases).

The CLI accepts the path to a [parameter file](../../parameter-file.md).  The parameter file is a YAML file instructing Delta Kusto on what job to perform.

A single call to Delta Kusto can run multiple jobs.  This feature enables change management on multi-tenant solutions within Azure Data Explorer.  In this tutorial we'll only run one job at the time.

A job consists of a current and target *sources*.  Sources can either be a script (or set of scripts) or an ADX Database.  Delta Kusto can therefore compute the delta between 2 ADX Databases, between a script and an ADX Database or between 2 scripts (offline mode).

A job also defines actions.  An action specify where to push the delta:  on the console, on files or on the current source (if the current source is an ADX Database).

This simple configuration enables multiple scenarios as we'll see in this tutorial.

The parameter file also defines a way for Delta Kusto to authenticate against ADX clusters via a Service Principal (see [Authenticating to ADX Clusters tutorial](../authentication/README.md) for details).

The parameter file is meant to be persisted in source control.  For that reason we do not want to put sensitive information such as credentials.  We can override parameter file values with parameters to the CLI to avoid relying on the file containing sensitive data.  Overriding values can also be useful to re-use the same parameter file in different environments (e.g. by overriding the cluster URIs / database name).

In this tutorial, we are going to use only one cluster but Delta Kusto can compute delta between multiple clusters.

## Scenarios

All scenarios we are going to cover are based on the following setup:  a dev database and a prod database.

![Setup](setup.png)

Those databases represent the same database in 2 different environments.  We'll use the same cluster but they could be on different ADX clusters.

All scenarios we'll look at will involve change management between those two environments.

## Original development

To simulate development happening before the introduction of Delta Kusto, we'll run the scripts from [dev-start-samples.kql](dev-start-samples.kql):

```sql
.create-or-alter function with (docstring = "A list of interesting states",folder = "Helpers") InterestingStates {
    dynamic(["WASHINGTON", "FLORIDA", "GEORGIA", "NEW YORK"])
}

.create-or-alter function  Add(a:real,b:real) {a+b}

.create-or-alter function with (docstring = "Direct table access example") DirectTableAccess(myTable:(*)) {
    myTable | count
}
```

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

