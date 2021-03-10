using DeltaKustoLib;
using System;
using System.Linq;

namespace DeltaKustoIntegration.Parameterization
{
    public class SourceParameterization
    {
        public DatabaseSourceParameterization? Database { get; set; } = null;

        public SourceFileParametrization[]? Scripts { get; set; } = null;

        internal void Validate()
        {
            if (Database != null && Scripts != null)
            {
                throw new DeltaException(
                    "Both 'database' and 'scripts' can't both be populated in a source");
            }
            if (Database == null && Scripts == null)
            {
                throw new DeltaException(
                    "Either 'database' or 'scripts' must be populated in a source");
            }
            if (Database != null)
            {
                Database.Validate();
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