using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Functions.EmptyCurrent
{
    public class FunctionEmptyCurrentTest : IntegrationTestBase
    {
        [Fact]
        public async Task EmptyDelta()
        {
            var paramPath = "Functions/EmptyCurrent/EmptyDelta/empty-delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(commands);
        }

        [Fact]
        public async Task OneFunctionDelta()
        {
            var paramPath = "Functions/EmptyCurrent/OneFunctionDelta/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var inputPath = parameters.Jobs!.First().Value.Target!.Scripts!.First().FilePath!;
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var inputCommands = await LoadScriptAsync(paramPath, inputPath);
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.True(inputCommands.SequenceEqual(outputCommands));
        }

        [Fact]
        public async Task TwoFunctionsDelta()
        {
            var paramPath = "Functions/EmptyCurrent/TwoFunctionsDelta/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var inputPath = parameters.Jobs!.First().Value.Target!.Scripts!.First().FilePath!;
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var inputCommands = await LoadScriptAsync(paramPath, inputPath);
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.True(inputCommands.SequenceEqual(outputCommands));
        }

        private CancellationToken CreateCancellationToken() =>
           new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
    }
}