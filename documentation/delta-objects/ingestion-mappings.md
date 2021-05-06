# Delta on ingestion mappings

This page documents how a delta is computed on ingestion mappings.

## Model

An ingestion mapping consists of:

* Mapping Name
* Mapping Kind
* Mapping Formatted as JSON

## Inputs

The only input for ingestion mappings are:

* [.create ingestion mapping](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-ingestion-mapping-command)
* [.create-or-alter ingestion mapping](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-ingestion-mapping-command)

## Delta

Kusto Command|Condition
-|-
[.create-or-alter ingestion mapping](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-ingestion-mapping-command)|Mapping exists in the target but either doesn't exist in the current or is different.
[.drop ingestion mapping](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/drop-ingestion-mapping-command)|Mapping exists in the current but doesn't in the target.