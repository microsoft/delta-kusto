using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration.TokenProvider
{
    public interface ITokenProviderFactory
    {
        ITokenProvider? CreateProvider(
            ITracer tracer,
            TokenProviderParameterization? parameterization);
    }
}