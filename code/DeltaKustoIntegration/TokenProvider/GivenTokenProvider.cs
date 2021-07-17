using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.TokenProvider
{
    internal class GivenTokenProvider : ITokenProvider
    {
        private readonly ITracer _tracer;
        private readonly IImmutableDictionary<string, string> _tokenMap;

        public GivenTokenProvider(ITracer tracer, IEnumerable<TokenParameterization> tokens)
        {
            _tracer = tracer;
            _tokenMap = tokens
                .ToImmutableDictionary(t => t.ClusterUri!, t => t.Token!);
        }

        Task<string> ITokenProvider.GetTokenAsync(string resource, CancellationToken ct)
        {
            if (_tokenMap.ContainsKey(resource))
            {
                _tracer.WriteLine(true, $"Token was provided for {resource}");

                return Task.FromResult(_tokenMap[resource]);
            }
            else
            {
                throw new DeltaException($"No token was provided for {resource}");
            }
        }
    }
}