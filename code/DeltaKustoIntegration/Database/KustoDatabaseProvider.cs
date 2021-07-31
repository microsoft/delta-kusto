using DeltaKustoIntegration.Kusto;
using DeltaKustoLib;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Database
{
    public class KustoDatabaseProvider : IDatabaseProvider
    {
        private readonly ITracer _tracer;
        private readonly IKustoManagementGateway _kustoManagementGateway;

        public KustoDatabaseProvider(
            ITracer tracer,
            IKustoManagementGateway kustoManagementGateway)
        {
            _tracer = tracer;
            _kustoManagementGateway = kustoManagementGateway;
        }

        async Task<DatabaseModel> IDatabaseProvider.RetrieveDatabaseAsync(
            CancellationToken ct)
        {
            _tracer.WriteLine(true, "Retrieve Kusto DB start");

            var commands = await _kustoManagementGateway.ReverseEngineerDatabaseAsync(ct);

            _tracer.WriteLine(true, "Retrieve Kusto DB end");

            return DatabaseModel.FromCommands(commands);
        }
    }
}