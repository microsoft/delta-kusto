using DeltaKustoLib.CommandModel;
using DeltaKustoLib.SchemaObjects;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Kusto
{
    public interface IKustoManagementGateway
    {
        Task<DatabaseSchema> GetDatabaseSchemaAsync(CancellationToken ct = default);

        Task ExecuteCommandsAsync(
            IEnumerable<CommandBase> commands,
            CancellationToken ct = default);
    }
}