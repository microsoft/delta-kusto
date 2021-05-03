using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Mappings
{
    public class MappingTest : IntegrationTestBase
    {
        [Fact]
        public async Task NoneToOneDelta()
        {
            var parameters = await RunParametersAsync(
                "Mappings/NoneToOne/delta-params.yaml",
                CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(outputPath);

            Assert.Single(outputCommands);

            var createMappingCommand = outputCommands
                .Where(c => c is CreateMappingCommand)
                .Cast<CreateMappingCommand>()
                .FirstOrDefault();

            Assert.NotNull(createMappingCommand);
            Assert.Equal("my-mapping", createMappingCommand!.MappingName.Text);
        }

        [Fact]
        public async Task TwoFunctionsDelta()
        {
            var parameters = await RunParametersAsync(
                "Functions/EmptyCurrent/TwoFunctionsDelta/delta-params.yaml",
                CreateCancellationToken());
            var inputPath = parameters.Jobs!.First().Value.Target!.Scripts!.First().FilePath!;
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var inputCommands = await LoadScriptAsync(inputPath);
            var outputCommands = await LoadScriptAsync(outputPath);

            Assert.True(inputCommands.SequenceEqual(outputCommands));
        }

        private CancellationToken CreateCancellationToken() =>
           new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
    }
}