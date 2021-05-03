using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Mappings
{
    public class MappingTest : IntegrationTestBase
    {
        [Fact]
        public async Task NoneToOne()
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
        public async Task OneToTwo()
        {
            var parameters = await RunParametersAsync(
                "Mappings/OneToTwo/delta-params.yaml",
                CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(outputPath);

            Assert.Equal(2, outputCommands.Count());

            var createMappingCommands = outputCommands
                .Where(c => c is CreateMappingCommand)
                .Cast<CreateMappingCommand>();

            Assert.Equal(2, createMappingCommands.Count());

            var kinds = createMappingCommands
                .Select(c => c.MappingKind)
                .ToImmutableHashSet();

            Assert.Contains("csv", kinds);
            Assert.Contains("json", kinds);
        }

        private CancellationToken CreateCancellationToken() =>
           new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
    }
}