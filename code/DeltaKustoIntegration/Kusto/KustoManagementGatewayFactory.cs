using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration.Kusto
{
    public class KustoManagementGatewayFactory : IKustoManagementGatewayFactory
    {
        private readonly ITracer _tracer;
        private readonly SimpleHttpClientFactory _httpClientFactory;

        public KustoManagementGatewayFactory(
            ITracer tracer,
            SimpleHttpClientFactory httpClientFactory)
        {
            _tracer = tracer;
            _httpClientFactory = httpClientFactory;
        }

        public IKustoManagementGateway CreateGateway(
            Uri clusterUri,
            string database,
            ITokenProvider tokenProvider)
        {
            return new KustoManagementGateway(
                clusterUri,
                database,
                tokenProvider,
                _tracer,
                _httpClientFactory);
        }
    }
}