using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
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
            TokenProviderParameterization tokenProvider);
    }
}