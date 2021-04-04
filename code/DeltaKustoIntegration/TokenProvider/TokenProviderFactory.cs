using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration.TokenProvider
{
    public class TokenProviderFactory : ITokenProviderFactory
    {
        private readonly ITracer _tracer;
        private readonly SimpleHttpClientFactory _httpClientFactory;

        public TokenProviderFactory(
            ITracer tracer,
            SimpleHttpClientFactory httpClientFactory)
        {
            _tracer = tracer;
            _httpClientFactory = httpClientFactory;
        }

        ITokenProvider? ITokenProviderFactory.CreateProvider(
            TokenProviderParameterization? parameterization)
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
                        _tracer,
                        _httpClientFactory,
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