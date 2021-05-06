# Delta objects

Delta Kusto compute a delta between two Kusto *sources* (either an actual ADX database or a KQL script) and produces a *delta* KQL script.

## Model

Delta Kusto first build a *database model* for each source, i.e. the *current* and the *target*, and compute the delta between those two.

## Inputs

The inputs for a model is the actual source:  either an ADX database or a KQL script (or scripts).

In the case of scripts, Delta Kusto doesn't accept every command related to an object.  It typically only accept one command per main object.  For instance, only `.create function` and `.create-or-alter function` are accepted ; `.alter function`, `.alter function docstring` etc. aren't accepted.

This is both for simplicity but also because Delta Kusto doesn't enforce a script order.  In order to compute a model, we would need to know, for instance, if `.create-or-alter function` or `.alter function docstring` was called first.

## Objects

This page documents how those delta are computed on different objects:

* [Functions](functions.md)
* [Tables](tables.md)