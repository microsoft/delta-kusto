using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Action
{
    public interface IActionProvider
    {
        Task ProcessDeltaCommandsAsync(
            bool doNotProcessIfDataLoss,
            ActionCommandCollection commands,
            CancellationToken ct = default);
    }
}