using DeltaKustoIntegration.Parameterization;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration.TokenProvider
{
    public interface ITokenProviderFactory
    {
        ITokenProvider? CreateProvider(TokenProviderParameterization? parameterization);
    }
}