#!/bin/bash

##########################################################################
##  Deploys Kusto Delta API Azure infrastructure solution
##
##  Parameters:
##
##  1- Name of resource group

rg=$1

echo "Resource group:  $rg"
echo "Current directory:  $(pwd)"

echo
echo "Deploying ARM template"

az deployment group create -n "deploy-$(uuidgen)" -g $rg \
    --template-file api-infra-deploy.json
