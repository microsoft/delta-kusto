using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration
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