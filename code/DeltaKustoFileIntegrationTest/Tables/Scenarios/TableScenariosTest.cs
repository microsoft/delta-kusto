using DeltaKustoLib.CommandModel;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.EmptyTarget
{
    public class TableScenariosTest : IntegrationTestBase
    {
        [Fact]
        public async Task AddFolderOnTable()
        {
            var parameters = await RunParametersAsync(
                "Tables/Scenarios/AddFolderOnTable/delta-params.yaml",
                CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Single(commands);

            var createTable = (CreateTableCommand)commands.First();

            Assert.NotNull(createTable.Folder);
        }

        [Fact]
        public async Task AddDocStringOnTable()
        {
            var parameters = await RunParametersAsync(
                "Tables/Scenarios/AddDocStringOnTable/delta-params.yaml",
                CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Single(commands);

            var createTable = (CreateTableCommand)commands.First();

            Assert.NotNull(createTable.DocString);
        }

        [Fact]
        public async Task ChangeColumnType()
        {
            var parameters = await RunParametersAsync(
                "Tables/Scenarios/ChangeColumnType/delta-params.yaml",
                CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Single(commands);

            var alterColumnType = (AlterColumnTypeCommand)commands.First();

            Assert.Equal("a", alterColumnType.ColumnName.Name);
        }

        [Fact]
        public async Task DropColumn()
        {
            var parameters = await RunParametersAsync(
                "Tables/Scenarios/DropColumn/delta-params.yaml",
                CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Single(commands);

            var dropColumns = (DropTableColumnsCommand)commands.First();

            Assert.Single(dropColumns.ColumnNames);
            Assert.Equal("a", dropColumns.ColumnNames.First().Name);
        }

        [Fact]
        public async Task ChangeColumnDocString()
        {
            var parameters = await RunParametersAsync(
                "Tables/Scenarios/ChangeColumnDoc/delta-params.yaml",
                CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Single(commands);

            var alterMerge = (AlterMergeTableColumnDocStringsCommand)commands.First();

            Assert.Equal(3, alterMerge.Columns.Count);

            foreach (var col in alterMerge.Columns)
            {
                Assert.Equal($"new param {col.ColumnName}", col.DocString.Text);
            }
        }

        [Fact]
        public async Task DropColumnDocString()
        {
            var parameters = await RunParametersAsync(
                "Tables/Scenarios/DropColumnDoc/delta-params.yaml",
                CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Single(commands);

            var alterMerge = (AlterMergeTableColumnDocStringsCommand)commands.First();

            Assert.Equal(3, alterMerge.Columns.Count);

            foreach (var col in alterMerge.Columns)
            {
                Assert.Equal(QuotedText.Empty, col.DocString);
            }
        }

        private CancellationToken CreateCancellationToken() =>
           new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
    }
}