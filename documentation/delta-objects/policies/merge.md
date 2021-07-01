# Delta on Merge Policy

This page documents how a delta is computed on [Merge Policy](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/merge-policy).

## Model

A merge policy consists of:

* Entity Type (either *database* or *table*)
* Entity Name
* `RowCountUpperBoundForMerge` (integer)
* `OriginalSizeMBUpperBoundForMerge` (integer)
* `MaxExtentsToMerge` (integer)
* `LoopPeriod` (timespan)
* `MaxRangeInHours` (integer)
* `AllowRebuild` (boolean)
* `AllowMerge` (boolean)
* `LookbackKind` (object)
* `LookbackCustomPeriod` (object)

## Inputs

* [.alter database / table policy merge](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/merge-policy#alter-policy)
* [.delete database / table policy merge](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/merge-policy#delete-policy-of-merge)

## Delta

Kusto Command|Condition
-|-
[.alter database / table policy merge](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/merge-policy#alter-policy)|Target's merge policy is different than current's (or current has none).
[.delete database / table policy merge](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/merge-policy#delete-policy-of-merge)|Current has merge policy while target doesn't.

