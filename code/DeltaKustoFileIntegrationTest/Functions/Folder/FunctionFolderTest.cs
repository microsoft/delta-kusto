using DeltaKustoLib.CommandModel;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoFileIntegrationTest.Functions.Folder
{
    public class FunctionFolderTest : IntegrationTestBase
    {
        [Fact]
        public async Task SimpleFolder()
        {
            await TestFunctionWithFolderAsync(
                "Functions/Folder/SimpleFolder/simple-folder-params.yaml",
                "simple");
        }

        [Fact]
        public async Task DeepFolder()
        {
            await TestFunctionWithFolderAsync(
                "Functions/Folder/DeepFolder/deep-folder-params.yaml",
                "root/branch/sub");
        }

        [Fact]
        public async Task SpacedFolder()
        {
            await TestFunctionWithFolderAsync(
                "Functions/Folder/SpacedFolder/spaced-folder-params.yaml",
                "my folder");
        }

        [Fact]
        public async Task FolderVsFile()
        {
            var paramPath = "Functions/Folder/FolderVsFile/folder-vs-file-params.yaml";
            var parameters = await RunParametersAsync(paramPath);
            var outputPath = parameters.Jobs!.First().Value.Action!.FilePath!;
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Empty(outputCommands);
        }

        private async Task TestFunctionWithFolderAsync(string paramPath, string folderPath)
        {
            var parameters = await RunParametersAsync(paramPath);
            var inputPath = parameters.Jobs!.First().Value.Target!.Scripts!.First().FilePath!;
            var inputCommands = await LoadScriptAsync(paramPath, inputPath);

            Assert.Single(inputCommands);
            Assert.IsType<CreateFunctionCommand>(inputCommands.First());

            var inputFunction = (CreateFunctionCommand)inputCommands.First();

            var outputRootPath = parameters.Jobs!.First().Value.Action!.FolderPath!;
            var outputPath = Path.Combine(
                outputRootPath,
                $"functions/create/{folderPath}/{inputFunction.FunctionName}.kql");
            var outputCommands = await LoadScriptAsync(paramPath, outputPath);

            Assert.Single(outputCommands);
            Assert.IsType<CreateFunctionCommand>(outputCommands.First());

            var outputFunction = (CreateFunctionCommand)outputCommands.First();

            Assert.Equal(inputFunction, outputFunction);
        }
    }
}