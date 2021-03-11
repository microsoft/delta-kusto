using DeltaKustoIntegration.TokenProvider;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration
{
    public interface IKustoManagementGatewayFactory
    {
        IKustoManagementGateway CreateGateway(
            string clusterUri,
            string database,
            ITokenProvider tokenProvider);
    }
}