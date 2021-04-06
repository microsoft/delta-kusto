# Delta Kusto Overview Tutorial

In this tutorial, we are going to use Delta Kusto and tour its funcitonalities.  This is going to cover a few scenarios.

Because of this breath we won't cover details such as how to download Delta Kusto or authentication to ADX Clusters.  We suggest looking at [other tutorials](README.md) for more detailed tasks.

*   Recommend other tutorials
*   Explain CLI
* Explain parameter file
* Explain overrides
* Using one cluster but could be many
    * Hint to multi-tenant scenario
* Specific diagrams to scenario

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

