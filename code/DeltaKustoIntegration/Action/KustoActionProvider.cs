using DeltaKustoIntegration.Kusto;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Action
{
    public class KustoActionProvider : IActionProvider
    {
        private readonly IKustoManagementGateway _kustoManagementGateway;

        public KustoActionProvider(IKustoManagementGateway kustoManagementGateway)
        {
            _kustoManagementGateway = kustoManagementGateway;
        }

        async Task IActionProvider.ProcessDeltaCommandsAsync(IEnumerable<CommandBase> commands)
        {
            await _kustoManagementGateway.ExecuteCommandsAsync(
                commands.OfType<DropFunctionCommand>());
            await _kustoManagementGateway.ExecuteCommandsAsync(
                commands.OfType<CreateFunctionCommand>());
        }
    }
}