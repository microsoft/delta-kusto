using DeltaKustoLib;
using System;

namespace DeltaKustoIntegration.Parameterization
{
    public class AdxSourceParameterization
    {
        public string? ClusterUri { get; set; } = null;

        public string? Database { get; set; } = null;

        internal void Validate()
        {
            Uri? uri;

            if (string.IsNullOrWhiteSpace(ClusterUri))
            {
                throw new DeltaException("'clusterUri' must be populated in a ADX source");
            }
            if (!Uri.TryCreate(ClusterUri!, UriKind.Absolute, out uri))
            {
                throw new DeltaException($"'clusterUri' is an invalid Uri:  '{ClusterUri}'");
            }
            if (uri.Scheme != "https")
            {
                throw new DeltaException($"'clusterUri' should be https but isn't:  '{ClusterUri}'");
            }
            if (uri.LocalPath != "/")
            {
                throw new DeltaException($"'clusterUri' should be domain name only but isn't:  '{ClusterUri}'");
            }

            if (string.IsNullOrWhiteSpace(Database))
            {
                throw new DeltaException("'database' must be populated in a ADX source");
            }
        }
    }
}