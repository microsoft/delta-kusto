using DeltaKustoLib.KustoModel;
using DeltaKustoLib.SchemaObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Database
{
    public class EmptyDatabaseProvider : IDatabaseProvider
    {
        Task<DatabaseModel> IDatabaseProvider.RetrieveDatabaseAsync(
            CancellationToken ct)
        {
            var db = DatabaseModel.FromDatabaseSchema(new DatabaseSchema());

            return Task.FromResult(db);
        }
    }
}