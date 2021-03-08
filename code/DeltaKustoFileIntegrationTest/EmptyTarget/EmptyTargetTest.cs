using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.EmptyTarget
{
    public class EmptyTargetTest : IntegrationTestBase
    {
        [Fact]
        public async Task EmptyDelta()
        {
            var parameters = await RunParametersAsync(
                "EmptyTarget/EmptyDelta/empty-delta-params.json");
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Empty(commands);
        }
    }
}