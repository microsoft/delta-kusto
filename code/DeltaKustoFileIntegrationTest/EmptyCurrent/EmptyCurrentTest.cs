using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.EmptyTarget
{
    public class EmptyCurrentTest : IntegrationTestBase
    {
        [Fact]
        public async Task EmptyDelta()
        {
            var parameters = await RunParametersAsync(
                "EmptyCurrent/EmptyDelta/empty-delta-params.json");
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Empty(commands);
        }

        [Fact]
        public async Task OneFunctionDelta()
        {
            var parameters = await RunParametersAsync(
                "EmptyCurrent/OneFunctionDelta/delta-params.json");
            var inputPath = parameters.Jobs!.First().Value.Target!.Scripts!.First().FilePath!;
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var inputCommands = await LoadScriptAsync(inputPath);
            var outputCommands = await LoadScriptAsync(outputPath);

            Assert.True(inputCommands.SequenceEqual(outputCommands));
        }
    }
}