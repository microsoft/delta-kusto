using DeltaKustoIntegration.Parameterization;
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

        public KustoManagementGatewayFactory(ITracer tracer)
        {
            _tracer = tracer;
        }

        public IKustoManagementGateway CreateGateway(
            Uri clusterUri,
            string database,
            TokenProviderParameterization tokenProvider)
        {
            return new KustoManagementGateway(
                clusterUri,
                database,
                tokenProvider,
                _tracer);
        }
    }
}