using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Kusto
{
    public interface IKustoManagementGateway
    {
        Task<bool> DoesDatabaseExistsAsync(CancellationToken ct = default);
     
        Task<IImmutableList<CommandBase>> ReverseEngineerDatabaseAsync(CancellationToken ct = default);

        Task ExecuteCommandsAsync(
            IEnumerable<CommandBase> commands,
            CancellationToken ct = default);
    }
}