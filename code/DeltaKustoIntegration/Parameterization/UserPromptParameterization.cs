using DeltaKustoLib;
using System;

namespace DeltaKustoIntegration.Parameterization
{
    public class UserPromptParameterization
    {
        public string? TenantId { get; set; }

        public string? UserId { get; set; }

        internal void Validate()
        {
        }
    }
}