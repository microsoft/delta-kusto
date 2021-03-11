using DeltaKustoIntegration.Parameterization;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration.TokenProvider
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
                if (parameterization.Login != null)
                {
                    throw new NotImplementedException();
                }
                else if (parameterization.TokenMap != null)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    throw new NotSupportedException("We should never reach that point");
                }
            }
        }
    }
}