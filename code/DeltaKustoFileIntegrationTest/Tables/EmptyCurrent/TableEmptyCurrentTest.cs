using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Tables.EmptyCurrent
{
    public class TableEmptyCurrentTest : IntegrationTestBase
    {
        [Fact]
        public async Task OneTableDelta()
        {
            var paramPath = "Tables/EmptyCurrent/OneTableDelta/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var inputPath = parameters.Jobs!.First().Value.Target!.Scripts!.First().FilePath!;
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var inputCommands = await LoadScriptAsync(paramPath, inputPath);
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.True(inputCommands.SequenceEqual(outputCommands));
        }

        [Fact]
        public async Task TwoFunctionsDelta()
        {
            var paramPath = "Tables/EmptyCurrent/TwoTablesDelta/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var inputPath = parameters.Jobs!.First().Value.Target!.Scripts!.First().FilePath!;
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var inputCommands = await LoadScriptAsync(paramPath, inputPath);
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);
            //  Create collection in order to get a consistent sort between the two
            var inputCollection = new CommandCollection(false, inputCommands);
            var outputCollection = new CommandCollection(false, outputCommands);

            Assert.True(inputCollection.AllCommands.SequenceEqual(outputCollection.AllCommands));
        }
    }
}