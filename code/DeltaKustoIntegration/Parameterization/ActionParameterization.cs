using DeltaKustoLib;
using System;
using System.Runtime;

namespace DeltaKustoIntegration.Parameterization
{
    public class ActionParameterization
    {
        public string? FilePath { get; set; } = null;

        public string? FolderPath { get; set; } = null;
        
        public string? CsvPath { get; set; } = null;

        public bool UsePluralForms { get; set; } = false;

        public bool PushToConsole { get; set; } = false;

        public bool PushToCurrent { get; set; } = false;

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(FilePath)
                && string.IsNullOrWhiteSpace(FolderPath)
                && string.IsNullOrWhiteSpace(CsvPath)
                && !PushToCurrent
                && !PushToConsole)
            {
                throw new DeltaException("No action defined");
            }
            if (!string.IsNullOrWhiteSpace(FilePath) && !string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new DeltaException(
                    "Both 'filePath' and 'folderPath' can't both be populated");
            }
            if (!string.IsNullOrWhiteSpace(FilePath) && !string.IsNullOrWhiteSpace(CsvPath))
            {
                throw new DeltaException(
                    "Both 'filePath' and 'csvPath' can't both be populated");
            }
            if (!string.IsNullOrWhiteSpace(FolderPath) && !string.IsNullOrWhiteSpace(CsvPath))
            {
                throw new DeltaException(
                    "Both 'folderPath' and 'csvPath' can't both be populated");
            }
        }
    }
}