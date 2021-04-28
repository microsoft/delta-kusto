using DeltaKustoLib;
using System;
using System.Linq;

namespace DeltaKustoIntegration.Parameterization
{
    public class SourceParameterization
    {
        public AdxSourceParameterization? Adx { get; set; } = null;

        public SourceFileParametrization[]? Scripts { get; set; } = null;

        public string? JsonFilePath { get; set; } = null;

        internal void Validate()
        {
            if (Adx != null && Scripts != null)
            {
                throw new DeltaException(
                    "Both 'adx' and 'scripts' can't both be populated in a source");
            }
            if (Adx != null && JsonFilePath != null)
            {
                throw new DeltaException(
                    "Both 'adx' and 'jsonFilePath' can't both be populated in a source");
            }
            if (Scripts != null && JsonFilePath != null)
            {
                throw new DeltaException(
                    "Both 'scripts' and 'jsonFilePath' can't both be populated in a source");
            }
            if (Adx == null && Scripts == null && JsonFilePath == null)
            {
                throw new DeltaException(
                    "Either 'adx', 'scripts' or 'jsonFilePath' must be populated in a source");
            }
            if (Adx != null)
            {
                try
                {
                    Adx.Validate();
                }
                catch (DeltaException ex)
                {
                    throw new DeltaException("Issue with 'adx'", ex);
                }
            }
            else if (JsonFilePath != null)
            {
                if (string.IsNullOrWhiteSpace(JsonFilePath))
                {
                    throw new DeltaException("'jsonFilePath' can't be empty");
                }
            }
            else if (!Scripts!.Any())
            {
                throw new DeltaException("'scripts' can't be empty in a source");
            }
            else
            {
                try
                {
                    foreach (var f in Scripts!)
                    {
                        f.Validate();
                    }
                }
                catch (DeltaException ex)
                {
                    throw new DeltaException("Issue with 'scripts'", ex);
                }
            }
        }
    }
}