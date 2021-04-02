using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration.Kusto
{
    public class KustoManagementGatewayFactory : IKustoManagementGatewayFactory
    {
        public IKustoManagementGateway CreateGateway(
            ITracer tracer,
            Uri clusterUri,
            string database,
            ITokenProvider tokenProvider)
        {
            return new KustoManagementGateway(tracer, clusterUri, database, tokenProvider);
        }
    }
}