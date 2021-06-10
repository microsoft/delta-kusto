# Delta on Update Policy

This page documents how a delta is computed on Update Policy.

## Model

An update policy consists of:

* Table Name
* List of [policy objects](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/updatepolicy#the-update-policy-object):
    * Is enabled
    * Source
    * Query
    * Is transactional
    * Propagate Ingestion Properties

## Inputs

Inputs for update policy are:

* [.alter table policy update](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/update-policy#alter-update-policy)

## Delta

Kusto Command|Condition
-|-
[.alter table policy update](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/update-policy#alter-update-policy)|Update Policies for the given table are different in target and current.  This command can be used to create, alter or delete (i.e. alter to an empty collection) policy objects.  All policy objects are overridden.