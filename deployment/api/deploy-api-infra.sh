#!/bin/bash

##########################################################################
##  Deploys Kusto Delta API Azure infrastructure solution
##
##  Parameters:
##
##  1- Name of resource group
##  2- Frontdoor's name

rg=$1
fd=$2

echo "Resource group:  $rg"
echo "Frontdoor's name:  $fd"
echo "Current directory:  $(pwd)"

echo
echo "Deploying ARM template"

az deployment group create -n "deploy-$(uuidgen)" -g $rg \
    --template-file api-infra-deploy.bicep \
    --parameters frontDoorName=$fd
