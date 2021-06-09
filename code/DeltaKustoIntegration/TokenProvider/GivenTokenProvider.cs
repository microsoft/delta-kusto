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
        private readonly IImmutableDictionary<Uri, string> _tokenMap;

        public GivenTokenProvider(ITracer tracer, IEnumerable<TokenParameterization> tokens)
        {
            _tracer = tracer;
            _tokenMap = tokens
                .ToImmutableDictionary(t => new Uri(t.ClusterUri!), t => t.Token!);
        }

        Task<string> ITokenProvider.GetTokenAsync(Uri clusterUri, CancellationToken ct)
        {
            if (_tokenMap.ContainsKey(clusterUri))
            {
                _tracer.WriteLine(true, $"Token was provided for {clusterUri}");

                return Task.FromResult(_tokenMap[clusterUri]);
            }
            else
            {
                throw new DeltaException($"No token was provided for {clusterUri}");
            }
        }
    }
}