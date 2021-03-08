using delta_kusto;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest
{
    public abstract class IntegrationTestBase
    {
        protected async Task<int> RunMainAsync(params string[] args)
        {
            var returnedValue = await Program.Main(args);

            return returnedValue;
        }

        protected async Task RunSuccessfulMainAsync(params string[] args)
        {
            var returnedValue = await RunMainAsync(args);

            Assert.Equal(0, returnedValue);
        }

        protected async Task<MainParameterization> RunParametersAsync(string parameterFilePath)
        {
            var returnedValue = await RunMainAsync("-p", parameterFilePath);

            Assert.Equal(0, returnedValue);

            var parameters = await new DeltaOrchestration().LoadParameterizationAsync(parameterFilePath);

            return parameters;
        }

        protected async Task<IImmutableList<CommandBase>> LoadScriptAsync(string scriptPath)
        {
            var script = await File.ReadAllTextAsync(scriptPath);
            var commands = CommandBase.FromScript(script);

            return commands;
        }
    }
}