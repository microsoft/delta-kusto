using DeltaKustoIntegration.Database;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest.Functions
{
    public class FunctionsAdxToFileTest : AdxIntegrationTestBase
    {
        [Fact]
        public async Task AdxToFile()
        {
            await LoopThroughStateFilesAsync(async (fromFile, toFile) =>
            {
                await PrepareCurrentAsync(fromFile);

                var outputPath = "outputs/functions/adx-to-file/"
                    + Path.GetFileNameWithoutExtension(fromFile)
                    + "_"
                    + Path.GetFileNameWithoutExtension(toFile)
                    + ".kql";
                var overrides = CurrentDbOverrides
                    .Append(("jobs.main.target.scripts", new[] { new { filePath = toFile } }))
                    .Append(("jobs.main.action.filePath", outputPath));
                var parameters = await RunParametersAsync(
                    "Functions/adx-to-file-params.json",
                    overrides);
                var outputCommands = await LoadScriptAsync(outputPath);
                var targetCommands = CommandBase.FromScript(
                    await File.ReadAllTextAsync(toFile));
                var finalCommands = await ApplyCommandsToCurrentAsync(outputCommands);

                Assert.True(
                    finalCommands.SequenceEqual(targetCommands),
                    $"From {fromFile} to {toFile}");
            });
        }

        [Fact]
        public async Task AdxToAdx()
        {
            await LoopThroughStateFilesAsync(async (fromFile, toFile) =>
            {
                await PrepareCurrentAsync(fromFile);

                var overrides = CurrentDbOverrides
                    .Concat(TargetDbOverrides);
                var parameters = await RunParametersAsync(
                    "Functions/adx-to-adx-params.json",
                    overrides);
                var targetCommands = CommandBase.FromScript(
                    await File.ReadAllTextAsync(toFile));
                var finalCommands = await GetTargetCommandsAsync();

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
                    await CleanDatabasesAsync();
                    await loopFunction(fromFile, toFile);
                }
            }
        }

        private async Task<IImmutableList<CommandBase>> ApplyCommandsToCurrentAsync(
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

        private async Task<IImmutableList<CommandBase>> GetTargetCommandsAsync()
        {
            var gateway = CreateKustoManagementGateway(false);
            var dbProvider = (IDatabaseProvider)new KustoDatabaseProvider(gateway);
            var emptyProvider = (IDatabaseProvider)new EmptyDatabaseProvider();

            //  Delta target with empty to get target
            var targetDb = await dbProvider.RetrieveDatabaseAsync();
            var emptyDb = await emptyProvider.RetrieveDatabaseAsync();
            var targetCommands = targetDb.ComputeDelta(emptyDb);

            return targetCommands;
        }
    }
}