using DeltaKustoIntegration.Kusto;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Text;
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

        async Task<DatabaseModel> IDatabaseProvider.RetrieveDatabaseAsync()
        {
            var databaseSchema = await _kustoManagementGateway.GetDatabaseSchemaAsync();

            return DatabaseModel.FromDatabaseSchema(databaseSchema);
        }
    }
}