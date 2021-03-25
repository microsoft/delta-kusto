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
                Adx.Validate();
            }
            else if(!Scripts!.Any())
            {
                throw new DeltaException("'scripts' can't be empty in a source");
            }
            else
            {
                foreach (var f in Scripts!)
                {
                    f.Validate();
                }
            }
        }
    }
}