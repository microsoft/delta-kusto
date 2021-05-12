using DeltaKustoLib.CommandModel;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Functions.WithCurrent
{
    public class FunctionWithCurrentTest : IntegrationTestBase
    {
        [Fact]
        public async Task EmptyBoth()
        {
            var paramPath = "Functions/WithCurrent/EmptyBoth/delta-params.json";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(commands);
        }

        [Fact]
        public async Task Same()
        {
            var paramPath = "Functions/WithCurrent/Same/delta-params.json";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(commands);
        }

        [Fact]
        public async Task TargetMore()
        {
            var paramPath = "Functions/WithCurrent/TargetMore/delta-params.json";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(commands);

            var function = (CreateFunctionCommand)commands.First();

            Assert.Equal("YourFunction", function.FunctionName.Name);
        }

        [Fact]
        public async Task TargetLess()
        {
            var paramPath = "Functions/WithCurrent/TargetLess/delta-params.json";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var outputRootFolder = parameters.Jobs!.First().Value.Action!.FolderPath!;
            var outputPath = Path.Combine(outputRootFolder, "functions/drop/YourFunction.kql");
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(commands);

            var function = (DropFunctionCommand)commands.First();

            Assert.Equal("YourFunction", function.FunctionName.Name);
        }

        private CancellationToken CreateCancellationToken() =>
           new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
    }
}