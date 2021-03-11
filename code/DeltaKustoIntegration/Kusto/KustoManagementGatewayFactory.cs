using DeltaKustoIntegration.TokenProvider;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration.Kusto
{
    public class KustoManagementGatewayFactory : IKustoManagementGatewayFactory
    {
        public IKustoManagementGateway CreateGateway(
            string clusterUri,
            string database,
            ITokenProvider tokenProvider)
        {
             throw new NotImplementedException();
        }
    }
}