using DeltaKustoIntegration.Database;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
        public async Task GenericAdxToFile()
        {
            await LoopThroughStateFilesAsync(async (fromFile, toFile) =>
            {
                await PrepareCurrentAsync(fromFile);

                var outputPath = "outputs/functions/adx-to-file/"
                    + Path.GetFileNameWithoutExtension(fromFile)
                    + "_"
                    + Path.GetFileNameWithoutExtension(toFile)
                    + ".kql";
                var overrides = new[]
                {
                        ("jobs.main.target.scripts", (object) new[]{ new { filePath = toFile } }),
                        ("jobs.main.action.filePath", outputPath)
                    };
                var parameters = await RunParametersAsync(
                    "Functions/AdxToFile/delta-params.json",
                    overrides);
                var outputCommands = await LoadScriptAsync(outputPath);
                var targetCommands = CommandBase.FromScript(
                    await File.ReadAllTextAsync(toFile));
                var finalCommands = await ApplyCommandsToCurrent(outputCommands);

                Assert.True(
                    finalCommands.SequenceEqual(targetCommands),
                    $"From {fromFile} to {toFile}");
            });
        }

        private async Task LoopThroughStateFilesAsync(Func<string, string, Task> loopFunction)
        {
            var stateFiles = Directory.GetFiles("Functions/States");

            foreach (var fromFile in stateFiles)
            {
                foreach (var toFile in stateFiles)
                {
                    await loopFunction(fromFile, toFile);
                }
            }
        }

        private async Task<IImmutableList<CommandBase>> ApplyCommandsToCurrent(
    IEnumerable<CommandBase> outputCommands)
        {
            var gateway = CreateKustoManagementGateway(true);
            var dbProvider = (IDatabaseProvider)new KustoDatabaseProvider(gateway);
            var emptyProvider = (IDatabaseProvider)new EmptyDatabaseProvider();
            //  Apply delta to current
            await gateway.ExecuteCommandsAsync(outputCommands);

            var finalDb = await dbProvider.RetrieveDatabaseAsync();
            var emptyDb = await emptyProvider.RetrieveDatabaseAsync();
            var finalCommands = finalDb.ComputeDelta(emptyDb);

            return finalCommands;
        }
    }
}