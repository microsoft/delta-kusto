using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.JsonSchema
{
    public class JsonSchemaTest : IntegrationTestBase
    {
        [Fact]
        public async Task HelpSamples()
        {
            var parameters = await RunParametersAsync(
                "JsonSchema/HelpSamples/delta-params.yaml",
                CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(outputPath);
            //  Mostly a check we can process the json file
            var oneTable = outputCommands
                .Where(c => c is CreateTableCommand)
                .Cast<CreateTableCommand>()
                .Where(c => c.TableName.Name == "table041121");

            Assert.Single(oneTable);
        }
 
        private CancellationToken CreateCancellationToken() =>
            new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
    }
}