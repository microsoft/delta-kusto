using DeltaKustoLib;
using System;

namespace DeltaKustoIntegration.Parameterization
{
    public class ServicePrincipalLoginParameterization
    {
        public string? ClientId { get; set; }
        
        public string? Secret { get; set; }

        internal void Validate()
        {
            if (ClientId == null)
            {
                throw new DeltaException("'clientId' must be populated in login");
            }
            if (Secret == null)
            {
                throw new DeltaException("'secret' must be populated in login");
            }
        }
    }
}