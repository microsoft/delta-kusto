using DeltaKustoIntegration.Kusto;
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
        private readonly IKustoManagementGateway _kustoManagementGateway;

        public KustoDatabaseProvider(IKustoManagementGateway kustoManagementGateway)
        {
            _kustoManagementGateway = kustoManagementGateway;
        }

        async Task<DatabaseModel> IDatabaseProvider.RetrieveDatabaseAsync(
            CancellationToken ct)
        {
            var databaseSchema = await _kustoManagementGateway.GetDatabaseSchemaAsync(ct);

            return DatabaseModel.FromDatabaseSchema(databaseSchema);
        }
    }
}