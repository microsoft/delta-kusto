using DeltaKustoLib;
using System;
using System.Linq;

namespace DeltaKustoIntegration.Parameterization
{
    public class SourceParameterization
    {
        public AdxSourceParameterization? Adx { get; set; } = null;

        public SourceFileParametrization[]? Scripts { get; set; } = null;

        internal void Validate()
        {
            if (Adx != null && Scripts != null)
            {
                throw new DeltaException(
                    "Both 'adx' and 'scripts' can't both be populated in a source");
            }
            if (Adx == null && Scripts == null)
            {
                throw new DeltaException(
                    "Either 'adx' or 'scripts' must be populated in a source");
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