using DeltaKustoLib;
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
        private readonly ITracer _tracer;
        private readonly bool _isVerbose;

        public ConsoleActionProvider(ITracer tracer, bool isVerbose)
        {
            _tracer = tracer;
            _isVerbose = isVerbose;
        }

        Task IActionProvider.ProcessDeltaCommandsAsync(
            bool doNotProcessIfDataLoss,
            ActionCommandCollection commands,
            CancellationToken ct)
        {
            foreach(var c in commands)
            {
                _tracer.WriteLine(_isVerbose, c.ToScript());
            }

            return Task.CompletedTask;
        }
    }
}