# Delta object

Delta Kusto compute a delta between two Kusto *sources* (either an actual ADX database or a KQL script) and produces a *delta* KQL script.

Delta Kusto first build a *database model* for each source, i.e. the *current* and the *target*, and compute the delta between those two.

This page documents how those delta are computed on different objects:

* [Functions](functions.md)
* [Tables](tables.md)