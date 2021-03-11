using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest
{
    public class FunctionsAdxToFileTest : AdxIntegrationTestBase
    {
        public FunctionsAdxToFileTest()
            : base(true, false)
        {
        }

        [Fact]
        public async Task FromEmptyDbToEmptyScript()
        {
            var parameters = await RunParametersAsync(
                "Functions/AdxToFile/FromEmptyDb/delta-params.json");
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Empty(commands);
        }
    }
}