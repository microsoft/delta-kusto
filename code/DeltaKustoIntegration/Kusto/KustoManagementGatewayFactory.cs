using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration.Kusto
{
    public class KustoManagementGatewayFactory : IKustoManagementGatewayFactory
    {
        private readonly TokenProviderParameterization _tokenProvider;
        private readonly ITracer _tracer;

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
            return new KustoManagementGateway(
                clusterUri,
                database,
                _tokenProvider,
                _tracer);
        }
    }
}