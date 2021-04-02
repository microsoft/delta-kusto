using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoIntegration.Kusto
{
    public interface IKustoManagementGatewayFactory
    {
        IKustoManagementGateway CreateGateway(
            ITracer tracer,
            Uri clusterUri,
            string database,
            ITokenProvider tokenProvider);
    }
}