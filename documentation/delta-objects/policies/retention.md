# Delta on Retention Policy

This page documents how a delta is computed on [Retention Policy](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/retentionpolicy).

## Model

A retention policy consists of:

* Entity Type (either *database* or *table*)
* Entity Name
* `SoftDeletePeriod` (timespan)
* `Recoverability` (boolean)

## Inputs

Inputs for retention policy are:

* [.alter database / table policy retention](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/retention-policy#alter-retention-policy)
* [.alter tables policy retention](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/retention-policy#alter-retention-policy)
* [.delete database / table policy retention](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/retention-policy#delete-retention-policy)

## Delta

Kusto Command|Condition
-|-
[.alter table policy retention](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/retention-policy#alter-retention-policy)|Target's retention policy is different than current's (or current has none).
[.alter tables policy retention](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/retention-policy#alter-retention-policy)|Plural form
[.delete policy retention](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/retention-policy#delete-retention-policy)|Current has retention policy while target doesn't.

"Plural form" (e.g. `.alter tables` vs `.alter table`) are used to bundle commands together when sent to an ADX Database.  "Singular form" is used when sent to files or console.  Plural form is only possible for identical policies among many tables.
