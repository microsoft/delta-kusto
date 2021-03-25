using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Action
{
    public class ConsoleActionProvider : IActionProvider
    {
        Task IActionProvider.ProcessDeltaCommandsAsync(
            bool doNotProcessIfDrops,
            ActionCommandCollection commands,
            CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}