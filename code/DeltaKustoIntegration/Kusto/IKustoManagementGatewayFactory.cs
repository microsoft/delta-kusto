using DeltaKustoIntegration.TokenProvider;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration.Kusto
{
    public interface IKustoManagementGatewayFactory
    {
        IKustoManagementGateway CreateGateway(
            Uri clusterUri,
            string database,
            ITokenProvider tokenProvider);
    }
}