![delta-kusto](delta-kusto.png)

# delta-kusto

Command-line interface (CLI) enabling CI / CD automation with Kusto objects (e.g. tables, functions, policies, security roles, etc.)
in [Azure Data Explorer](https://docs.microsoft.com/en-us/azure/data-explorer/data-explorer-overview) (ADX) databases.

delta-kusto aims at doing what [SQL Database projects](https://docs.microsoft.com/en-us/sql/ssdt/project-oriented-offline-database-development) do for Microsoft SQL:  enabling CI/CD, change management and source control of Kusto databases.

## Overview

The high-level view of delta-kusto is the following:

![Overview diagram](documentation/overview.png)

The green boxes represent [sources](documentation/sources.md).  A source can be:

* ADX Database
* Kusto scripts (either a kusto script file or a hierarchy of folders containing kusto scripts)

delta-script computes the *delta* between the two sources.  The *delta* can be represented as a Kusto script containing the kusto commands required to run on the *current source* so it would be identical to *target source*.  For instance the delta 

This *delta script* can either be applied directly to an ADX Database or saved as a Kusto script for human validation.

Using different combinations of sources can enable different scenarios.

### ADX Database (current) to Kusto scripts (target)

Typical CI / CD

### Kusto scripts (current) to ADX Database (target)

Siphon out a Kusto DB content.

### Kusto scripts (current) to Kusto scripts (target)

Offline sync.

### ADX Database (current) to ADX Database (target)

Live Sync.