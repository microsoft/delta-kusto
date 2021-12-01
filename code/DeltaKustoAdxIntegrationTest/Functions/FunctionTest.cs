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
        public async Task ToScriptFolder()
        {
            var scriptPath = "Functions/ScriptFolder/target.kql";
            var paramsPath = "Functions/ScriptFolder/folder-params.yaml";
            var dbName = await InitializeDbAsync();

            await PrepareDbAsync(scriptPath, dbName);

            var overrides = ImmutableArray<(string path, string value)>
                .Empty
                .Add(("jobs.main.target.adx.clusterUri", ClusterUri.ToString()))
                .Add(("jobs.main.target.adx.database", dbName));
            var parameters = await RunParametersAsync(paramsPath, overrides);
            var outputPath = parameters.Jobs.First().Value.Action!.FolderPath!;
            var outputCommands = await LoadScriptAsync(
                paramsPath,
                Path.Combine(outputPath, "functions/create/root/branch/sub/MyFunction.kql"));

            Assert.Single(outputCommands);
            Assert.IsType<CreateFunctionCommand>(outputCommands.First());
        }
    }
}