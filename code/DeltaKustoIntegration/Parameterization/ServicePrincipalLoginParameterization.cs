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
            if (string.IsNullOrWhiteSpace(TenantId))
            {
                throw new DeltaException("'tenantId' must be populated in login");
            }
            if (string.IsNullOrWhiteSpace(ClientId))
            {
                throw new DeltaException("'clientId' must be populated in login");
            }
            if (string.IsNullOrWhiteSpace(Secret))
            {
                throw new DeltaException("'secret' must be populated in login");
            }
        }
    }
}