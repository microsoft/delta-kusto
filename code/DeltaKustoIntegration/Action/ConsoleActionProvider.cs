using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Action
{
    public class ConsoleActionProvider : IActionProvider
    {
        Task IActionProvider.ProcessDeltaCommandsAsync(IEnumerable<CommandBase> commands)
        {
            throw new NotImplementedException();
        }
    }
}