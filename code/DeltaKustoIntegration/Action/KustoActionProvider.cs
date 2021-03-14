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
            //  Re-order the commands in the order we want them to execute
            //  (essentially, drop before create)
            var sortedCommands =
                ((IEnumerable<CommandBase>)commands.OfType<DropFunctionCommand>())
                .Concat(commands.OfType<CreateFunctionCommand>());

            await _kustoManagementGateway.ExecuteCommandsAsync(sortedCommands);
        }
    }
}