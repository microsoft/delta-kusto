using DeltaKustoIntegration.Parameterization;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration
{
    public interface ITokenProviderFactory : ITokenProvider
    {
        ITokenProvider? CreateProvider(TokenProviderParameterization? parameterization);
    }
}