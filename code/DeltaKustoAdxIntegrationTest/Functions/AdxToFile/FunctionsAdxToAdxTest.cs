using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest
{
    public class FunctionsAdxToAdxTest : AdxIntegrationTestBase
    {
        [Fact]
        public async Task FromEmptyDb()
        {
            var parameters = await RunParametersAsync(
                "Functions/AdxToFile/FromEmptyDb/delta-params.json");
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Empty(commands);
        }
    }
}