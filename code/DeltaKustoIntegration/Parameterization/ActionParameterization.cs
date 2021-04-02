using DeltaKustoLib;
using System;
using System.Runtime;

namespace DeltaKustoIntegration.Parameterization
{
    public class ActionParameterization
    {
        public string? FilePath { get; set; } = null;

        public string? FolderPath { get; set; }

        public bool PushToConsole { get; set; } = false;

        public bool PushToCurrentCluster { get; set; } = false;

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(FilePath)
                && string.IsNullOrWhiteSpace(FolderPath)
                && !PushToCurrentCluster
                && !PushToConsole)
            {
                throw new DeltaException("No action defined");
            }
            if (!string.IsNullOrWhiteSpace(FilePath) && !string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new DeltaException(
                    "Both 'filePath' and 'folderPath' can't both be populated");
            }
        }
    }
}