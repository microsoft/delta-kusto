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
                    return new LoginTokenProvider(
                        parameterization.Login!.TenantId!,
                        parameterization.Login!.ClientId!,
                        parameterization.Login!.Secret!);
                }
                else if (parameterization.Tokens != null)
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