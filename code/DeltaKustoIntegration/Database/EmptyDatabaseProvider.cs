using DeltaKustoLib.CommandModel;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Database
{
    public class EmptyDatabaseProvider : IDatabaseProvider
    {
        private static readonly DatabaseModel EMPTY_MODEL = DatabaseModel.FromCommands(new CommandBase[0]);

        Task<DatabaseModel> IDatabaseProvider.RetrieveDatabaseAsync(
            CancellationToken ct)
        {
            return Task.FromResult(EMPTY_MODEL);
        }
    }
}