using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Action
{
    public interface IActionProvider
    {
        Task ProcessDeltaCommandsAsync(bool doNotProcessIfDrops, ActionCommandCollection commands);
    }
}