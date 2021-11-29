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
    public class FunctionTest : AdxAutoIntegrationTestBase
    {
        protected override string StatesFolderPath => "Functions";

        [Fact]
        public async Task ToFolder()
        {
            var scriptPath = "Functions/Folder/target.kql";
            var dbName = await InitializeDbAsync();

            await PrepareDbAsync(scriptPath, dbName);

            var overrides = ImmutableArray<(string path, string value)>
                .Empty
                .Add(("jobs.main.target.adx.clusterUri", ClusterUri.ToString()))
                .Add(("jobs.main.target.adx.database", dbName));
            var parameters = await RunParametersAsync(
                "Functions/Folder/folder-params.yaml",
                overrides);
            var outputPath = parameters.Jobs.First().Value.Action!.FolderPath!;
            var outputCommands = await LoadScriptAsync(
                outputPath,
                "");

            throw new NotImplementedException();

            //var targetFileCommands = CommandBase.FromScript(
            //    await File.ReadAllTextAsync(toFile));

            //await Task.WhenAll(
            //    ApplyCommandsAsync(outputCommands, currentDbName),
            //    ApplyCommandsAsync(targetFileCommands, dbName));

            //var finalCommandsTask = FetchDbCommandsAsync(currentDbName);
            //var targetAdxCommands = await FetchDbCommandsAsync(dbName);
            //var finalCommands = await finalCommandsTask;
            //var targetModel = DatabaseModel.FromCommands(targetAdxCommands);
            //var finalModel = DatabaseModel.FromCommands(finalCommands);
            //var finalScript = string.Join(";\n\n", finalCommands.Select(c => c.ToScript()));
            //var targetScript = string.Join(";\n\n", targetFileCommands.Select(c => c.ToScript()));

            //Assert.True(
            //    finalModel.Equals(targetModel),
            //    $"From {fromFile} to {toFile}:\n\n{finalScript}\nvs\n\n{targetScript}");
            //AdxDbTestHelper.Instance.ReleaseDb(currentDbName);
        }
    }
}