using DeltaKustoIntegration.Parameterization;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration
{
    public class TokenProviderFactory : ITokenProviderFactory
    {
        ITokenProvider? ITokenProviderFactory.CreateProvider(TokenProviderParameterization? parameterization)
        {
            if (parameterization == null)
            {
                return null;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}