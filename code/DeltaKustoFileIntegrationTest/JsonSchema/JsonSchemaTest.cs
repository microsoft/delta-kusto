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
            var paramPath = "JsonSchema/HelpSamples/delta-params.yaml";
            var parameters = await RunParametersAsync(paramPath, CreateCancellationToken());
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);
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