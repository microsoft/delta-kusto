using DeltaKustoLib;
using System;

namespace DeltaKustoIntegration.Parameterization
{
    public class TokenMapParameterization
    {
        public string? ClusterUri { get; set; }
        
        public string? Token { get; set; }
        
        internal void Validate()
        {
            if (ClusterUri == null)
            {
                throw new DeltaException("'clusterUri' must be populated in tokenMap");
            }
            if (Token == null)
            {
                throw new DeltaException("'token' must be populated in tokenMap");
            }
        }
    }
}