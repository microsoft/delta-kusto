#!/bin/bash

##########################################################################
##  Create parameter file
##
##  Parameters:
##
##  1- DB Count
##  2- Cluster URI
##  3- Script file name (not .kql)

dbCount=$1
clusterUri=$2
fileName=$3

# echo "DB Count:  $dbCount"
# echo "Cluster URI:  $clusterUri"

#   Output header
cat parameters-headers.yaml

#   Loop for databases
for i in $(seq -f "%08g" 1 $dbCount)
do
    #   Output for one db
    escapedClusterUri=$(printf '%s\n' "$clusterUri" | sed -e 's/[]\/$*.^[]/\\&/g');
    sed "s/db-name/dN$i/g; s/cluster-uri/$escapedClusterUri/g; s/script-file/$fileName/g" parameters-db.yaml
done