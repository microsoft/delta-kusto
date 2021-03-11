using DeltaKustoIntegration.TokenProvider;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration.Kusto
{
    public class KustoManagementGatewayFactory : IKustoManagementGatewayFactory
    {
        public IKustoManagementGateway CreateGateway(
            Uri clusterUri,
            string database,
            ITokenProvider tokenProvider)
        {
            return new KustoManagementGateway(clusterUri, database, tokenProvider);
        }
    }
}