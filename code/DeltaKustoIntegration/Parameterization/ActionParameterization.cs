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
            if (FilePath == null
                && FolderPath == null
                && !PushToCurrentCluster
                && !PushToConsole)
            {
                throw new DeltaException("No action defined");
            }
            if (FilePath != null && FolderPath != null)
            {
                throw new DeltaException(
                    "Both 'filePath' and 'folderPath' can't both be populated");
            }
            if (FilePath == null && FolderPath == null)
            {
                throw new DeltaException(
                    "Either 'filePath' and 'folderPath' must be populated");
            }
            if (FilePath != null && string.IsNullOrWhiteSpace(FilePath))
            {
                throw new DeltaException(
                    "If 'filePath' is specified, it must contain a valid path");
            }
            if (FolderPath != null && string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new DeltaException(
                    "If 'folderPath' is specified, it must contain a valid path");
            }
        }
    }
}