using DeltaKustoFileIntegrationTest;
using DeltaKustoIntegration.Database;
using DeltaKustoIntegration.Kusto;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest
{
    public abstract class AdxIntegrationTestBase : IntegrationTestBase
    {
        #region Inner Types
        private record TestCase(string FromFile, string ToFile, bool UsePluralForms)
        {
            public string GetOutputPath(string statesFolderPath) =>
                Path.Combine("outputs", statesFolderPath, "adx-to-adx/")
                + Path.GetFileNameWithoutExtension(FromFile)
                + "_2_"
                + Path.GetFileNameWithoutExtension(ToFile)
                + $"_{UsePluralForms}"
                + ".kql";
        }
        #endregion

        private readonly bool _overrideLoginTokenProvider;

        protected AdxIntegrationTestBase(bool overrideLoginTokenProvider = true)
        {
            var clusterUri = Environment.GetEnvironmentVariable("deltaKustoClusterUri");
            var tenantId = Environment.GetEnvironmentVariable("deltaKustoTenantId");
            var servicePrincipalId = Environment.GetEnvironmentVariable("deltaKustoSpId");
            var servicePrincipalSecret = Environment.GetEnvironmentVariable("deltaKustoSpSecret");

            if (string.IsNullOrWhiteSpace(clusterUri))
            {
                throw new ArgumentNullException(nameof(clusterUri));
            }
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }
            if (string.IsNullOrWhiteSpace(servicePrincipalId))
            {
                throw new ArgumentNullException(nameof(servicePrincipalId));
            }
            if (string.IsNullOrWhiteSpace(servicePrincipalSecret))
            {
                throw new ArgumentNullException(nameof(servicePrincipalSecret));
            }

            _overrideLoginTokenProvider = overrideLoginTokenProvider;
            ClusterUri = new Uri(clusterUri);
            TenantId = tenantId;
            ServicePrincipalId = servicePrincipalId;
            ServicePrincipalSecret = servicePrincipalSecret;
            KustoGatewayFactory = new KustoManagementGatewayFactory(
                new TokenProviderParameterization
                {
                    Login = new ServicePrincipalLoginParameterization
                    {
                        TenantId = TenantId,
                        ClientId = ServicePrincipalId,
                        Secret = ServicePrincipalSecret
                    }
                },
                Tracer,
                null);
        }

        protected Uri ClusterUri { get; }

        protected string TenantId { get; }

        protected string ServicePrincipalId { get; }

        protected string ServicePrincipalSecret { get; }

        protected IKustoManagementGatewayFactory KustoGatewayFactory { get; }

        protected static Task<DbNameHolder> InitializeDbAsync()
        {
            throw new NotImplementedException();
        }

        protected async Task<IImmutableList<string>> GetDbsAsync(int dbCount)
        {
            return await AdxDbTestHelper.Instance.GetDbsAsync(dbCount);
        }

        protected async Task CleanDbAsync(string dbName)
        {
            await AdxDbTestHelper.Instance.CleanDbAsync(dbName);
        }

        public void ReleaseDbs(IEnumerable<string> dbNames)
        {
            AdxDbTestHelper.Instance.ReleaseDbs(dbNames);
        }

        protected async Task TestAdxToFile(string statesFolderPath)
        {
            await LoopThroughStateFilesAsync(
                Path.Combine(statesFolderPath, "States"),
                async (fromFile, toFile, usePluralForms) =>
                {
                    using (var testDb = await InitializeDbAsync())
                    using (var currentDb = await InitializeDbAsync())
                    {
                        await PrepareDbAsync(fromFile, currentDb.Name);

                        var outputPath = Path.Combine("outputs", statesFolderPath, "adx-to-file/")
                            + Path.GetFileNameWithoutExtension(fromFile)
                            + "_2_"
                            + Path.GetFileNameWithoutExtension(toFile)
                            + ".kql";
                        var overrides = ImmutableArray<(string path, string value)>
                        .Empty
                        .Add(("jobs.main.current.adx.clusterUri", ClusterUri.ToString()))
                        .Add(("jobs.main.current.adx.database", currentDb.Name))
                        .Add(("jobs.main.target.scripts[0].filePath", toFile))
                        .Add(("jobs.main.action.filePath", outputPath))
                        .Add(("jobs.main.action.usePluralForms", usePluralForms));
                        var parameters = await RunParametersAsync(
                            "adx-to-file-params.json",
                            overrides);
                        var outputCommands = await LoadScriptAsync("", outputPath);
                        var targetFileCommands = CommandBase.FromScript(
                            await File.ReadAllTextAsync(toFile));

                        await Task.WhenAll(
                            ApplyCommandsAsync(outputCommands, currentDb.Name),
                            ApplyCommandsAsync(targetFileCommands, testDb.Name));

                        var finalCommandsTask = FetchDbCommandsAsync(currentDb.Name);
                        var targetAdxCommands = await FetchDbCommandsAsync(testDb.Name);
                        var finalCommands = await finalCommandsTask;
                        var targetModel = DatabaseModel.FromCommands(targetAdxCommands);
                        var finalModel = DatabaseModel.FromCommands(finalCommands);
                        var finalScript = string.Join(";\n\n", finalCommands.Select(c => c.ToScript()));
                        var targetScript = string.Join(";\n\n", targetFileCommands.Select(c => c.ToScript()));

                        Assert.True(
                            finalModel.Equals(targetModel),
                            $"From {fromFile} to {toFile}:\n\n{finalScript}\nvs\n\n{targetScript}");
                    }
                });
        }

        protected async Task TestFileToAdx(string statesFolderPath)
        {
            await LoopThroughStateFilesAsync(
                Path.Combine(statesFolderPath, "States"),
                async (fromFile, toFile, usePluralForms) =>
                {
                    using (var testDb = await InitializeDbAsync())
                    using (var targetDb = await InitializeDbAsync())
                    {
                        await PrepareDbAsync(toFile, targetDb.Name);

                        var outputPath = Path.Combine("outputs", statesFolderPath, "file-to-adx/")
                            + Path.GetFileNameWithoutExtension(fromFile)
                            + "_2_"
                            + Path.GetFileNameWithoutExtension(toFile)
                            + ".kql";
                        var overrides = ImmutableArray<(string path, string value)>
                            .Empty
                            .Add(("jobs.main.target.adx.clusterUri", ClusterUri.ToString()))
                            .Add(("jobs.main.target.adx.database", targetDb.Name))
                            .Add(("jobs.main.current.scripts[0].filePath", fromFile))
                            .Add(("jobs.main.action.filePath", outputPath))
                            .Add(("jobs.main.action.usePluralForms", usePluralForms));
                        var parameters = await RunParametersAsync(
                            "file-to-adx-params.json",
                            overrides);
                        var outputCommands = await LoadScriptAsync("", outputPath);
                        var currentCommands = CommandBase.FromScript(
                            await File.ReadAllTextAsync(fromFile));
                        var targetCommandsTask = FetchDbCommandsAsync(targetDb.Name);

                        await ApplyCommandsAsync(
                            currentCommands.Concat(outputCommands),
                            testDb.Name);

                        var targetCommands = await targetCommandsTask;
                        var finalCommands = await FetchDbCommandsAsync(testDb.Name);
                        var targetModel = DatabaseModel.FromCommands(targetCommands);
                        var finalModel = DatabaseModel.FromCommands(finalCommands);
                        var finalScript = string.Join(";\n\n", finalCommands.Select(c => c.ToScript()));
                        var targetScript = string.Join(";\n\n", targetCommands.Select(c => c.ToScript()));

                        Assert.True(
                            finalModel.Equals(targetModel),
                            $"From {fromFile} to {toFile}:\n\n{finalScript}\nvs\n\n{targetScript}");
                    }
                });
        }

        protected async Task TestAdxToAdx(string statesFolderPath)
        {
            var testCases = CreateTestCases(Path.Combine(statesFolderPath, "States"));
            var dbNames = await GetDbsAsync(2 * testCases.Count);

            try
            {
                var currentDbNames = dbNames.Take(testCases.Count).ToImmutableArray();
                var targetDbNames = dbNames.Skip(testCases.Count).ToImmutableArray();
                Func<int, Task> prepareDbs = async i =>
                {
                    await Task.WhenAll(
                        CleanDbAsync(currentDbNames[i]),
                        CleanDbAsync(targetDbNames[i]));
                    await Task.WhenAll(
                        PrepareDbAsync(testCases[i].FromFile, currentDbNames[i]),
                        PrepareDbAsync(testCases[i].ToFile, targetDbNames[i]));
                };
                var prepareDbTasks = Enumerable.Range(0, testCases.Count)
                    .Select(i => prepareDbs(i))
                    .ToImmutableArray();
                var overrides = Enumerable.Range(0, testCases.Count)
                    .SelectMany(i => ImmutableArray<(string path, string value)>
                    .Empty
                    .Add(($"jobs.job{i}.current.adx.clusterUri", ClusterUri.ToString()))
                    .Add(($"jobs.job{i}.current.adx.database", currentDbNames[i]))
                    .Add(($"jobs.job{i}.target.adx.clusterUri", ClusterUri.ToString()))
                    .Add(($"jobs.job{i}.target.adx.database", targetDbNames[i]))
                    .Add(($"jobs.job{i}.action.filePath", testCases[i].GetOutputPath(statesFolderPath)))
                    .Add(($"jobs.job{i}.action.usePluralForms", testCases[i].UsePluralForms.ToString().ToLower()))
                    .Add(($"jobs.job{i}.action.pushToCurrent", "true")))
                    .ToImmutableArray();

                await Task.WhenAll(prepareDbTasks);

                var parameters = await RunParametersAsync(
                    "adx-to-adx-params.json",
                    overrides);
                Func<int, Task> validateTestCase = async i =>
                {
                    var targetCommandsTask = FetchDbCommandsAsync(targetDbNames[i]);
                    var finalCommands = await FetchDbCommandsAsync(currentDbNames[i]);
                    var targetCommands = await targetCommandsTask;
                    var targetModel = DatabaseModel.FromCommands(targetCommands);
                    var finalModel = DatabaseModel.FromCommands(finalCommands);
                    var finalScript = string.Join(";\n\n", finalCommands.Select(c => c.ToScript()));
                    var targetScript = string.Join(";\n\n", targetCommands.Select(c => c.ToScript()));

                    Assert.True(
                        targetModel.Equals(finalModel),
                        $"From {testCases[i].FromFile} to {testCases[i].ToFile} "
                        + $"({testCases[i].UsePluralForms}):  "
                        + $"\n\n{finalScript}\nvs\n\n{targetScript}");
                };
                var validationTasks = Enumerable.Range(0, testCases.Count)
                    .Select(i => validateTestCase(i))
                    .ToImmutableArray();

                await Task.WhenAll(validationTasks);
            }
            finally
            {
                ReleaseDbs(dbNames);
            }
        }

        private async static Task LoopThroughStateFilesAsync(
            string folderPath,
            Func<string, string, string, Task> loopFunction)
        {
            var stateFiles = Directory.GetFiles(folderPath);

            foreach (var fromFile in stateFiles)
            {
                foreach (var toFile in stateFiles)
                {
                    foreach (var usePluralForms in new[] { true, false })
                    {
                        await loopFunction(
                            fromFile,
                            toFile,
                            usePluralForms.ToString().ToLower());
                    }
                }
            }
        }

        private static IImmutableList<TestCase> CreateTestCases(string folderPath)
        {
            var stateFiles = Directory.GetFiles(folderPath);
            var usePluralFormsSet = ImmutableArray<bool>.Empty.Add(true).Add(false);
            var testCases = stateFiles
                .SelectMany(fromFile => stateFiles.Select(toFile => (fromFile, toFile)))
                .SelectMany(pair => usePluralFormsSet.Select(b => new TestCase(
                    pair.fromFile,
                    pair.toFile,
                    b)))
                .ToImmutableArray();

            return testCases;
        }

        private async Task ApplyCommandsAsync(IEnumerable<CommandBase> commands, string dbName)
        {
            var gateway = KustoGatewayFactory.CreateGateway(ClusterUri, dbName);

            //  Apply commands to the db
            await gateway.ExecuteCommandsAsync(commands);
        }

        private async Task<IImmutableList<CommandBase>> FetchDbCommandsAsync(string dbName)
        {
            var gateway = KustoGatewayFactory.CreateGateway(ClusterUri, dbName);
            var dbProvider = (IDatabaseProvider)new KustoDatabaseProvider(
                new ConsoleTracer(false),
                gateway);
            var emptyProvider = (IDatabaseProvider)new EmptyDatabaseProvider();
            var finalDb = await dbProvider.RetrieveDatabaseAsync();
            var emptyDb = await emptyProvider.RetrieveDatabaseAsync();
            //  Use the delta from an empty db to get 
            var finalCommands = emptyDb.ComputeDelta(finalDb);

            return finalCommands;
        }

        protected override Task<MainParameterization> RunParametersAsync(
            string parameterFilePath,
            IEnumerable<(string path, string value)>? overrides = null)
        {
            var adjustedOverrides = overrides
                ?? ImmutableList<(string path, string value)>.Empty;

            if (_overrideLoginTokenProvider)
            {
                adjustedOverrides = adjustedOverrides
                    .Append(("tokenProvider.login.tenantId", TenantId))
                    .Append(("tokenProvider.login.clientId", ServicePrincipalId))
                    .Append(("tokenProvider.login.secret", ServicePrincipalSecret));
            }

            return base.RunParametersAsync(parameterFilePath, adjustedOverrides);
        }

        protected async Task PrepareDbAsync(string scriptPath, string dbName)
        {
            var script = await File.ReadAllTextAsync(scriptPath);

            try
            {
                var commands = CommandBase.FromScript(script);
                var gateway = KustoGatewayFactory.CreateGateway(ClusterUri, dbName);

                await gateway.ExecuteCommandsAsync(commands);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failure during PrepareDb.  dbName='{dbName}'.  "
                    + $"Script path = '{scriptPath}'.  "
                    + $"Script = '{script.Replace("\n", "\\n").Replace("\r", "\\r")}'",
                    ex);
            }
        }
    }
}