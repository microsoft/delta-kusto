using DeltaKustoLib;
using System;

namespace DeltaKustoIntegration.Parameterization
{
    public class ClusterSourceParameterization
    {
        public string? ClusterUri { get; set; } = null;

        public string? Database { get; set; } = null;

        internal void Validate()
        {
            if (ClusterUri == null)
            {
                throw new DeltaException("'clusterUri' must be populated in a cluster source");
            }
            if (!Uri.TryCreate(ClusterUri!, UriKind.Absolute, out _))
            {
                throw new DeltaException($"'clusterUri' is an invalid Uri:  '{ClusterUri}'");
            }
            if (Database == null)
            {
                throw new DeltaException("'database' must be populated in a cluster source");
            }
            if (string.IsNullOrWhiteSpace(Database))
            {
                throw new DeltaException($"'database' isn't valid:  '{Database}'");
            }
        }
    }
}