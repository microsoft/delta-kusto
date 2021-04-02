using DeltaKustoLib;
using System;

namespace DeltaKustoIntegration.Parameterization
{
    public class TokenParameterization
    {
        public string? ClusterUri { get; set; }

        public string? Token { get; set; }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(ClusterUri))
            {
                throw new DeltaException("'clusterUri' must be populated in tokenMap");
            }
            if (string.IsNullOrWhiteSpace(Token))
            {
                throw new DeltaException("'token' must be populated in tokenMap");
            }
        }
    }
}