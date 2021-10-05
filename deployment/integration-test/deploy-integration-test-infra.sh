#!/bin/bash

##########################################################################
##  Deploys Integration Test Azure infrastructure solution
##
##  Parameters:
##
##  1- Name of resource group
##  2- Tenant ID
##  3- Service Principal Client ID (which should be cluster admin)

rg=$1
tenantId=$2
spid=$3

echo "Resource group:  $rg"
echo "Tenant ID:  $tenantId"
echo "Service Principal's Client ID:  $spid"
echo "Current directory:  $(pwd)"

echo
echo "Deploying ARM template"

az deployment group create -n "deploy-$(uuidgen)" -g $rg \
    --template-file api-infra-deploy.json \
    --parameters tenantId=$tenantId clientId=$spid
