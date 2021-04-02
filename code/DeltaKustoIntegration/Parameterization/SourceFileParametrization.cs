using DeltaKustoLib;
using System;
using System.Linq;

namespace DeltaKustoIntegration.Parameterization
{
    public class SourceFileParametrization
    {
        public string? FilePath { get; set; } = null;

        public string? FolderPath { get; set; } = null;

        public string[]? Extensions { get; set; } = null;

        internal void Validate()
        {
            if (!string.IsNullOrWhiteSpace(FilePath) && !string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new DeltaException(
                    "Both 'filePath' and 'folderPath' can't both be populated");
            }
            if (string.IsNullOrWhiteSpace(FilePath) && string.IsNullOrWhiteSpace(FolderPath))
            {
                throw new DeltaException(
                    "Either 'filePath' and 'folderPath' must be populated");
            }
            if (Extensions != null && !Extensions.Any())
            {
                throw new DeltaException(
                    "If 'extensions' is specified, it must contain at least one extension");
            }
            if (!string.IsNullOrWhiteSpace(FilePath) && Extensions != null)
            {
                throw new DeltaException(
                    "'extensions' can't be specified in conjonction with 'filePath'");
            }
        }
    }
}