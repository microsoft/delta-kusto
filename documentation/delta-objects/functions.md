# Delta on functions

This page documents how a delta is computed on functions.

## Model

A function consists of:

* Function Name
* Function Parameters
* Function Body
* Function Folder
* Function Documentation (doc-string)

## Inputs

Inputs for functions are:

* [.create function](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-function)
* [.create-or-alter function](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-alter-function)

## Delta

Kusto Command|Condition
-|-
[.drop function](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/drop-function)|Function exists in the current but doesn't in the target.
[.drop functions](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/drop-function#drop-functions)|Plural form.
[.create-or-alter function](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-alter-function)|Function exists in the target but either doesn't exist in the current or is different.

"Plural form" (e.g. `.drop functions` vs `.drop function`) are used to bundle commands together when sent to an ADX Database.  "Singular form" is used when sent to files or console.
