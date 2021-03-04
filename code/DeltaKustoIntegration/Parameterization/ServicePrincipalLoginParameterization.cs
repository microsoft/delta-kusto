using DeltaKustoLib;
using System;

namespace DeltaKustoIntegration.Parameterization
{
    public class ServicePrincipalLoginParameterization
    {
        public string? TenantId { get; set; }
        
        public string? ClientId { get; set; }

        public string? Secret { get; set; }

        internal void Validate()
        {
            if (TenantId == null)
            {
                throw new DeltaException("'tenantId' must be populated in login");
            }
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