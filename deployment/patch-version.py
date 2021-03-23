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

    #   Add patch to the version
    version=document.getElementsByTagName('Version')[0]
    patchVersion = version.firstChild.nodeValue
    version.firstChild.nodeValue = patchVersion + "." + patchNumber
    versionParts = patchVersion.split('.')
    if len(versionParts)!=3:
        print("The project version should have three parts, e.g. 1.2.3 but doesn't:  " % patchVersion)

        exit(1)
    else:
        fullVersion = version.firstChild.nodeValue

        #   Partial versions
        minorVersion = versionParts[0] + '.' + versionParts[1]
        majorVersion = versionParts[0]

        #   Output project
        print('Project content:')
        print(document.toxml())
        writeXml(document, path)

        #   Output variable
        print('Set the full version in GitHub Action output:  %s' % fullVersion)
        print('##[set-output name=full-version;]%s' % fullVersion)
        print('Set the patch version in GitHub Action output:  %s' % patchVersion)
        print('##[set-output name=patch-version;]%s' % patchVersion)
        print('Set the major version in GitHub Action output:  %s' % minorVersion)
        print('##[set-output name=minor-version;]%s' % minorVersion)
        print('Set the minor version in GitHub Action output:  %s' % majorVersion)
        print('##[set-output name=major-version;]%s' % majorVersion)
