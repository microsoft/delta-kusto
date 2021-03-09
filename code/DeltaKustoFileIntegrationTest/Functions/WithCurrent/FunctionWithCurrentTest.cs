using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.EmptyTarget
{
    public class FunctionWithCurrentTest : IntegrationTestBase
    {
        [Fact]
        public async Task EmptyBoth()
        {
            var parameters = await RunParametersAsync(
                "Functions/WithCurrent/EmptyBoth/delta-params.json");
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Empty(commands);
        }

        [Fact]
        public async Task Same()
        {
            var parameters = await RunParametersAsync(
                "Functions/WithCurrent/Same/delta-params.json");
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Empty(commands);
        }

        [Fact]
        public async Task TargetMore()
        {
            var parameters = await RunParametersAsync(
                "Functions/WithCurrent/TargetMore/delta-params.json");
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Single(commands);

            var function = (CreateFunctionCommand)commands.First();

            Assert.Equal("YourFunction", function.FunctionName);
        }
    }
}