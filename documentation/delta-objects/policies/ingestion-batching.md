# Delta on IngestionBatching Policy

This page documents how a delta is computed on [Ingestion Batching Policy](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/batching-policy).

## Model

A retention policy consists of:

* Entity Type (either *database* or *table*)
* Entity Name
* `MaximumBatchingTimeSpan` (timespan)
* `MaximumNumberOfItems` (integer)
* `MaximumRawDataSizeMB` (integer)

## Inputs

Inputs for retention policy are:

* [.alter database / table policy ingestionbatching](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/batching-policy#altering-the-ingestionbatching-policy)
* [.alter tables policy ingestionbatching](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/batching-policy#altering-the-ingestionbatching-policy)
* [.delete database / table policy ingestionbatching](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/batching-policy#deleting-the-ingestionbatching-policy)

## Delta

Kusto Command|Condition
-|-
[.alter database / table policy ingestionbatching](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/batching-policy#altering-the-ingestionbatching-policy)|Target's ingestion batching policy is different than current's (or current has none).
[.alter tables policy ingestionbatching](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/batching-policy#altering-the-ingestionbatching-policy)|Plural form
[.delete database / table policy ingestionbatching](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/batching-policy#deleting-the-ingestionbatching-policy)|Current has ingestion batching policy while target doesn't.

"Plural form" (e.g. `.alter tables` vs `.alter table`) are used to bundle commands together when sent to an ADX Database.  "Singular form" is used when sent to files or console.  Plural form is only possible for identical policies among many tables.
