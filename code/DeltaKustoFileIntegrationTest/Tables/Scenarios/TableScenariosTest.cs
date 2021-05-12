using DeltaKustoLib.CommandModel;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Tables.Scenarios
{
    public class TableScenariosTest : IntegrationTestBase
    {
        [Fact]
        public async Task AddFolderOnTable()
        {
            var paramPath = "Tables/Scenarios/AddFolderOnTable/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(commands);

            var createTable = (CreateTableCommand)commands.First();

            Assert.NotNull(createTable.Folder);
        }

        [Fact]
        public async Task AddDocStringOnTable()
        {
            var paramPath = "Tables/Scenarios/AddDocStringOnTable/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(commands);

            var createTable = (CreateTableCommand)commands.First();

            Assert.NotNull(createTable.DocString);
        }

        [Fact]
        public async Task ChangeColumnType()
        {
            var paramPath = "Tables/Scenarios/ChangeColumnType/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(commands);

            var alterColumnType = (AlterColumnTypeCommand)commands.First();

            Assert.Equal("a", alterColumnType.ColumnName.Name);
        }

        [Fact]
        public async Task DropColumn()
        {
            var paramPath = "Tables/Scenarios/DropColumn/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(commands);

            var dropColumns = (DropTableColumnsCommand)commands.First();

            Assert.Single(dropColumns.ColumnNames);
            Assert.Equal("a", dropColumns.ColumnNames.First().Name);
        }

        [Fact]
        public async Task ChangeColumnDocString()
        {
            var paramPath = "Tables/Scenarios/ChangeColumnDoc/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(paramPath, outputPath);

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
            var paramPath = "Tables/Scenarios/DropColumnDoc/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(commands);

            var alterMerge = (AlterMergeTableColumnDocStringsCommand)commands.First();

            Assert.Equal(3, alterMerge.Columns.Count);

            foreach (var col in alterMerge.Columns)
            {
                Assert.Equal(QuotedText.Empty, col.DocString);
            }
        }

        [Fact]
        public async Task DropMultipleTablesTablesString()
        {
            var paramPath = "Tables/Scenarios/DropMultipleTables/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(commands);

            var dropTables = (DropTablesCommand)commands.First();

            Assert.Equal(3, dropTables.TableNames.Count);
        }

        private CancellationToken CreateCancellationToken() =>
           new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
    }
}