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
                await PrepareDbAsync(fromFile, true);

                var outputPath = "outputs/functions/adx-to-file/"
                    + Path.GetFileNameWithoutExtension(fromFile)
                    + "_2_"
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

                await ApplyCommandsAsync(outputCommands, true);

                var finalCommands = await FetchDbCommandsAsync(true);

                Assert.True(
                    finalCommands.SequenceEqual(targetCommands),
                    $"From {fromFile} to {toFile}");
            });
        }

        [Fact]
        public async Task FileToAdx()
        {
            await LoopThroughStateFilesAsync(async (fromFile, toFile) =>
            {
                await PrepareDbAsync(toFile, false);

                var outputPath = "outputs/functions/adx-to-file/"
                    + Path.GetFileNameWithoutExtension(fromFile)
                    + "_2_"
                    + Path.GetFileNameWithoutExtension(toFile)
                    + ".kql";
                var overrides = TargetDbOverrides
                    .Append(("jobs.main.current.scripts", new[] { new { filePath = fromFile } }))
                    .Append(("jobs.main.action.filePath", outputPath));
                var parameters = await RunParametersAsync(
                    "Functions/file-to-adx-params.json",
                    overrides);
                var outputCommands = await LoadScriptAsync(outputPath);
                var currentCommands = CommandBase.FromScript(
                    await File.ReadAllTextAsync(fromFile));
                var targetCommands = CommandBase.FromScript(
                    await File.ReadAllTextAsync(toFile));
                
                await ApplyCommandsAsync(
                    currentCommands.Concat(outputCommands),
                    true);

                var finalCommands = await FetchDbCommandsAsync(true);

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
                await Task.WhenAll(
                    PrepareDbAsync(fromFile, true),
                    PrepareDbAsync(toFile, false));

                var outputPath = "outputs/functions/adx-to-adx/"
                    + Path.GetFileNameWithoutExtension(fromFile)
                    + "_2_"
                    + Path.GetFileNameWithoutExtension(toFile)
                    + ".kql";
                var overrides = CurrentDbOverrides
                    .Concat(TargetDbOverrides)
                    .Append(("jobs.main.action.filePath", outputPath));
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

        private async Task ApplyCommandsAsync(IEnumerable<CommandBase> commands, bool isCurrent)
        {
            var gateway = CreateKustoManagementGateway(isCurrent);
            
            //  Apply commands to the db
            await gateway.ExecuteCommandsAsync(commands);
        }

        private async Task<IImmutableList<CommandBase>> FetchDbCommandsAsync(bool isCurrent)
        {
            var gateway = CreateKustoManagementGateway(isCurrent);
            var dbProvider = (IDatabaseProvider)new KustoDatabaseProvider(gateway);
            var emptyProvider = (IDatabaseProvider)new EmptyDatabaseProvider();
            var finalDb = await dbProvider.RetrieveDatabaseAsync();
            var emptyDb = await emptyProvider.RetrieveDatabaseAsync();
            //  Use the delta from an empty db to get 
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