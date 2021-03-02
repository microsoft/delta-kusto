using DeltaKustoLib;
using System;
using System.Linq;

namespace DeltaKustoIntegration.Parameterization
{
    public class SourceParameterization
    {
        public ClusterSourceParameterization? Cluster { get; set; } = null;

        public SourceFileParametrization[]? Scripts { get; set; } = null;

        internal void Validate()
        {
            if (Cluster != null && Scripts != null)
            {
                throw new DeltaException(
                    "Both 'cluster' and 'scripts' can't both be populated in a source");
            }
            if (Cluster == null && Scripts == null)
            {
                throw new DeltaException(
                    "Either 'cluster' or 'scripts' must be populated in a source");
            }
            if (Cluster != null)
            {
                Cluster.Validate();
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