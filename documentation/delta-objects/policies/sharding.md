# Delta on Sharding Policy

This page documents how a delta is computed on [Sharding Policy](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/sharding-policy).

## Model

A sharding policy consists of:

* Entity Type (either *database* or *table*)
* Entity Name
* `MaxRowCount` (integer)
* `MaxExtentSizeInMb` (integer)
* `MaxOriginalSizeInMb` (integer)
* `UseShardEngine` (boolean)
* `ShardEngineMaxRowCount` (integer)
* `ShardEngineMaxExtentSizeInMb` (integer)
* `ShardEngineMaxOriginalSizeInMb` (integer)

## Inputs

* [.alter database / table policy sharding](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/sharding-policy#alter-policy)
* [.delete database / table policy sharding](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/sharding-policy#delete-policy)

## Delta

Kusto Command|Condition
-|-
[.alter database / table policy sharding](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/sharding-policy#alter-policy)|Target's sharding policy is different than current's (or current has none).
[.delete database / table policy sharding](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/sharding-policy#delete-policy)|Current has sharding policy while target doesn't.

