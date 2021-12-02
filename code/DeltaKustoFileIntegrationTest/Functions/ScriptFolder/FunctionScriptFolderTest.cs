using DeltaKustoLib.CommandModel;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Functions.ScriptFolder
{
    public class FunctionScriptFolderTest : IntegrationTestBase
    {
        [Fact]
        public async Task ToScriptFolder()
        {
            var paramsPath = "Functions/ScriptFolder/ToScriptFolder/folder-params.yaml";
            var parameters =await RunParametersAsync(paramsPath);
            var inputPath = parameters.Jobs!.First().Value.Target!.Scripts!.First().FilePath!;
            var inputCommands = await LoadScriptAsync(paramsPath, inputPath);

            Assert.Single(inputCommands);
            Assert.IsType<CreateFunctionCommand>(inputCommands.First());

            var inputFunction = (CreateFunctionCommand)inputCommands.First();

            var outputPath = parameters.Jobs.First().Value.Action!.FolderPath!;
            var outputCommands = await LoadScriptAsync(
                paramsPath,
                Path.Combine(outputPath, "functions/create/root/branch_departments/sub/MyFunction.kql"));

            Assert.Single(outputCommands);
            Assert.IsType<CreateFunctionCommand>(outputCommands.First());

            Assert.Equal(inputFunction, outputCommands.First());
        }
    }
}