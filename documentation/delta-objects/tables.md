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
* [.create tables](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-tables-command)
* [.create-merge table](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-merge-table-command)
* [.create-merge tables](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-merge-tables-command)
* [.alter-merge table](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-merge-table-command)
* [.alter-merge table column-docstrings](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-merge-table-column)

The first 3 are considered equivalent to describe a table model while the 4th one only informs the column doc-string.

## Delta

Kusto Command|Potential Data Loss|Condition
-|-|-
[.drop table](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/drop-table-command)|X (can potentially be recovered with [.undo drop table](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/undo-drop-table-command))|Table exists in the current but doesn't in the target.
[.drop tables](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/drop-table-command)|X (can potentially be recovered with [.undo drop table](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/undo-drop-table-command))|Plural form.
[.create-merge table](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-merge-table-command)||Table exists in target but not current (create table).  Columns exist in target but not current (create columns).  Folder or doc-string are different in target and current (update folder and doc-string).
[.create-merge tables](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-merge-tables-command)||Plural form.
[.alter column](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-column)|X|Column exists in both current and target but have different type (change column type)
[.drop table columns](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/drop-column)|X|Columns exist in the target but either don't exist in the current or are different.
[.alter-merge table column-docstrings](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-merge-table-column)||Column doc-string is different between current and target.

"Plural form" (e.g. `.drop tables` vs `.drop table`) are used to bundle commands together when sent to an ADX Database.  "Singular form" is used when sent to files or console.  Plural form isn't always possible, e.g. when folder changes are necessary on different table, [.create-merge tables](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/create-merge-tables-command) can't be used in bulk.

It is important to note that it is currently impossible to re-order columns on a table with a single command.  Delta Kusto doesn't do column re-ordering and therefore configuration drift could occur between current and target regarding column order.