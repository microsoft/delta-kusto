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
            if (Extensions != null && !Extensions.Any())
            {
                throw new DeltaException(
                    "If 'extensions' is specified, it must contain at least one extension");
            }
            if (FilePath != null && Extensions != null)
            {
                throw new DeltaException(
                    "'extensions' can't be specified in conjonction with 'filePath'");
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