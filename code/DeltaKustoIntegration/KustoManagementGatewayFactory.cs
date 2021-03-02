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
            int? servicePrincipal)
        {
            throw new NotImplementedException();
        }
    }
}