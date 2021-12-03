using DeltaKustoLib;
using System;

namespace DeltaKustoIntegration.Parameterization
{
    public class UserManagedIdentityParameterization
    {
        public string? ClientId { get; set; }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(ClientId))
            {
                throw new DeltaException("'clientId' can't be empty");
            }
        }
    }
}