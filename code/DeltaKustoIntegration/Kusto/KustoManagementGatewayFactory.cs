using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration.Kusto
{
    public class KustoManagementGatewayFactory : IKustoManagementGatewayFactory
    {
        private readonly TokenProviderParameterization _tokenProvider;
        private readonly ITracer _tracer;
        private readonly IDictionary<(Uri, string), IKustoManagementGateway> _gatewayCache =
            new ConcurrentDictionary<(Uri clusterUri, string database), IKustoManagementGateway>();

        public KustoManagementGatewayFactory(
            TokenProviderParameterization tokenProvider,
            ITracer tracer)
        {
            _tokenProvider = tokenProvider;
            _tracer = tracer;
        }

        public IKustoManagementGateway CreateGateway(
            Uri clusterUri,
            string database)
        {
            var key = (clusterUri, database);
            IKustoManagementGateway? gateway;

            //  Make the gateways singleton as they hold Kusto-SDK connections
            if (!_gatewayCache.TryGetValue(key, out gateway))
            {
                lock (_gatewayCache)
                {
                    gateway = new KustoManagementGateway(
                        clusterUri,
                        database,
                        _tokenProvider,
                        _tracer);

                    _gatewayCache.Add(key, gateway);
                }
            }

            return gateway;
        }
    }
}