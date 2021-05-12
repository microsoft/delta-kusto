using DeltaKustoLib.CommandModel;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Tables.WithCurrent
{
    public class TableWithCurrentTest : IntegrationTestBase
    {
        [Fact]
        public async Task EmptyBoth()
        {
            var paramPath = "Tables/WithCurrent/EmptyBoth/delta-params.json";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(commands);
        }

        [Fact]
        public async Task Same()
        {
            var paramPath = "Tables/WithCurrent/Same/delta-params.json";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(commands);
        }

        [Fact]
        public async Task TargetMore()
        {
            var paramPath = "Tables/WithCurrent/TargetMore/delta-params.json";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(commands);

            var table = (CreateTableCommand)commands.First();

            Assert.Equal("your-table", table.TableName.Name);
        }

        [Fact]
        public async Task TargetLess()
        {
            var paramPath = "Tables/WithCurrent/TargetLess/delta-params.json";
            var parameters = await RunParametersAsync(paramPath);
            var outputRootFolder = parameters.Jobs!.First().Value.Action!.FolderPath!;
            var outputPath = Path.Combine(outputRootFolder, "tables/drop/your-table.kql");
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(commands);

            var table = (DropTableCommand)commands.First();

            Assert.Equal("your-table", table.TableName.Name);
        }
    }
}