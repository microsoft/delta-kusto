#!/bin/bash

##########################################################################
##  Turn an ADX cluster if not on already
##
##  Parameters:
##
##  1- Name of resource group
##  2- Test level

rg=$1
testLevel=$2

echo "Resource group:  $rg"
echo "Test Level:  $testLevel"

# Make sure Kusto extension is installed on CLI
az extension add -n kusto

# Retrieve cluster name
clusterName=$(az kusto cluster list -g $rg --query "[?tags.testLevel==$testLevel].name" -o tsv)
# Retrieve cluster state
state=$(az kusto cluster list -g $rg --query "[?tags.testLevel==$testLevel].state" -o tsv)

echo "Cluster Name:  $clusterName"
echo "State:  $state"

if [ "$state" -ne "running" ]
then
    # Actually start the cluster
    az kusto cluster start -n $clusterName -g $rg -n $clusterName
fi
