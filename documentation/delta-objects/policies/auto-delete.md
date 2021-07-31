# Delta on Auto Delete Policy

This page documents how a delta is computed on [Auto Delete Policy](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/auto-delete-policy-command).

## Model

An auto delete policy consists of:

* Table Name
* `ExpiryDate` (datetime)
* `DeleteIfNotEmpty` (boolean)

## Inputs

Inputs for retention policy are:

* [.alter table policy auto_delete](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/auto-delete-policy-command#alter-policy)
* [.delete table policy auto_delete](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/auto-delete-policy-command#delete-policy)

## Delta

Kusto Command|Condition
-|-
[.alter table policy auto_delete](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/auto-delete-policy-command#alter-policy)|Target's auto delete policy is different than current's (or current has none).
[.delete table policy auto_delete](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/auto-delete-policy-command#delete-policy)|Current has auto delete policy while target doesn't.

