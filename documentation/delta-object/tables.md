# Delta on tables

This page documents how a delta is computed on tables.

## Model

A table consists of:

* Table Name
* List of columns
  * Column Name
  * Primitive type
  * Documentation (doc-string) for the column
* Table Folder
* Table Documentation (doc-string)

## Inputs

Inputs for table model are:

* [.create table](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-table-command)
* [.create-merge table](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-merge-table-command)
* [.alter-merge table](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-merge-table-command)
* [.alter-merge table column-docstrings](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-merge-table-column)

The first 3 are considered equivalent to describe a table model while the 4th one only informs the column doc-string.

## Delta

Kusto Command|Potential Data Loss|Condition
-|-|-
[.drop table](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/drop-table-command)|X (can potentially be recovered with [.undo drop table](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/undo-drop-table-command))|Table exists in the current but doesn't in the target.
[.alter table docstring](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-table-docstring-command)||Table doc-string is different between current and target.
[.alter table folder](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-table-folder-command)||Table folder is different between current and target.
[.alter-merge table](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-merge-table-command)||Columns exist in target but not current (adding columns only).
[.create-merge table](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-merge-table-command)||Re-order columns
[.drop column](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/drop-column)|X|Column exists in the target but either doesn't exist in the current or is different.
[.alter-merge table column-docstrings](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-merge-table-column)||Column doc-string is different between current and target or column only exists in target.
[.alter column](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-column)|X|Column exists in both current and target but have different type (change column type)

In the case of tables, since they carry data that can be hard to retrieve (as opposed to functions, for instance), Delta-Kusto is conservative on the delta commands emmitted.  For instance, if only the `folder` is modified, [.alter table folder](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-table-folder-command) is used instead of [.alter-merge table](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-merge-table-command).