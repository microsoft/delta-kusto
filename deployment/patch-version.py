#!/usr/bin/python3

import sys
import os
import xml.dom.minidom as md 

def writeXml(document, path):
    with open(path, "w" ) as fs:  
        fs.write(document.toxml() ) 

if len(sys.argv) != 3:
    print("There are %d arguments" % (len(sys.argv)-1))
    print("Arguments should be")
    print("1-File path")
    print("2-Patch number")

    exit(1)
else:
    path = sys.argv[1]
    patchNumber = sys.argv[2]

    print ('Text file:  %s' % (path))
    print ('Patch Number:  %s' % (patchNumber))

    #   Load XML and print out
    document = md.parse(path)
    print('XML content:')
    print(document.toxml())

    #   Set GeneratePackageOnBuild to true
    generate=document.getElementsByTagName('GeneratePackageOnBuild')[0]
    generate.firstChild.nodeValue = "true"

    #   Add patch to the version
    version=document.getElementsByTagName('Version')[0]
    version.firstChild.nodeValue += "." + patchNumber
    fullVersion = version.firstChild.nodeValue

    #   Output project
    print('Project content:')
    print(document.toxml())
    writeXml(document, path)

    #   Output variable
    print('Set the full version in GitHub Action output:  %s' % fullVersion)
    print('##[set-output name=full-version;]%s' % fullVersion)