using DeltaKustoLib.CommandModel;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.EmptyTarget
{
    public class TableWithCurrentTest : IntegrationTestBase
    {
        [Fact]
        public async Task EmptyBoth()
        {
            var parameters = await RunParametersAsync(
                "Tables/WithCurrent/EmptyBoth/delta-params.json",
                CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Empty(commands);
        }

        [Fact]
        public async Task Same()
        {
            var parameters = await RunParametersAsync(
                "Tables/WithCurrent/Same/delta-params.json",
                CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Empty(commands);
        }

        [Fact]
        public async Task TargetMore()
        {
            var parameters = await RunParametersAsync(
                "Tables/WithCurrent/TargetMore/delta-params.json",
                CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Single(commands);

            var table = (CreateTableCommand)commands.First();

            Assert.Equal("YourFunction", table.TableName.Name);
        }

        [Fact]
        public async Task TargetLess()
        {
            var parameters = await RunParametersAsync(
                "Tables/WithCurrent/TargetLess/delta-params.json",
                CreateCancellationToken());
            var outputRootFolder = parameters.Jobs!.First().Value.Action!.FolderPath!;
            var outputPath = Path.Combine(outputRootFolder, "Tables/drop/YourFunction.kql");
            var commands = await LoadScriptAsync(outputPath);

            Assert.Single(commands);

            var table = (DropTableCommand)commands.First();

            Assert.Equal("YourFunction", table.TableName.Name);
        }

        private CancellationToken CreateCancellationToken() =>
           new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
    }
}