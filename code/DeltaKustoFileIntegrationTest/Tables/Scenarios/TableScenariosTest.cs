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

        private CancellationToken CreateCancellationToken() =>
           new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
    }
}