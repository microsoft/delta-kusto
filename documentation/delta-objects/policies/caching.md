# Delta on Caching Policy

This page documents how a delta is computed on [Caching Policy](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/cachepolicy).

## Model

A cache policy consists of:

* Entity type (either *Database* or *Table*)
* Entity name (either database name or table name)
* `HotData` (timespan)
* `HotIndex` (timespan)

The most common way to configure the policy is to set a unique `hot` timespan which sets both the `HotData` and `HotIndex`.

## Inputs

Inputs for caching policy are:

* [.alter database / table policy caching](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/cache-policy)

The *database name* for input commands isn't used by Delta Kusto.  It could be any valid name.

## Delta

Kusto Command|Condition
-|-
[.alter database / table policy caching](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/cache-policy)|Target's caching policy is different than current's (or current has none).  This command can be used to create or alter the policy.
[.delete database / table policy caching](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/cache-policy)|The target has no caching Policy while the current does.