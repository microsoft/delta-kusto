using DeltaKustoLib.CommandModel;
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
                "Functions/AdxToFile/FromEmptyDbToEmptyScript/delta-params.json");
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var commands = await LoadScriptAsync(outputPath);

            Assert.Empty(commands);
        }

        [Fact]
        public async Task FromEmptyDbToOneFunction()
        {
            var parameters = await RunParametersAsync(
                "Functions/AdxToFile/FromEmptyDbToOneFunction/delta-params.json");
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var inputPath = parameters.Jobs!.First().Value.Target!.Scripts!.First().FilePath!;
            var inputCommands = await LoadScriptAsync(inputPath);
            var outputCommands = await LoadScriptAsync(outputPath);

            Assert.True(inputCommands.SequenceEqual(outputCommands));
        }

        [Fact]
        public async Task FromEmptyDbToTwoFunctions()
        {
            var parameters = await RunParametersAsync(
                "Functions/AdxToFile/FromEmptyDbToTwoFunctions/delta-params.json");
            var inputPath = parameters.Jobs!.First().Value.Target!.Scripts!.First().FilePath!;
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var inputCommands = await LoadScriptAsync(inputPath);
            var outputCommands = await LoadScriptAsync(outputPath);

            Assert.True(inputCommands.SequenceEqual(outputCommands));
        }

        [Fact]
        public async Task FromOneToNone()
        {
            await PrepareCurrentAsync("Functions/AdxToFile/FromOneToNone/current.kql");

            var parameters = await RunParametersAsync(
                "Functions/AdxToFile/FromOneToNone/delta-params.json");
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(outputPath);

            Assert.Single(outputCommands);
            Assert.IsType<DropFunctionCommand>(outputCommands.First()!);
        }
    }
}