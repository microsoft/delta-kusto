using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.EmptyTarget
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

            //Assert.True(inputCommands.SequenceEqual(outputCommands));
        }
 
        private CancellationToken CreateCancellationToken() =>
            new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
    }
}