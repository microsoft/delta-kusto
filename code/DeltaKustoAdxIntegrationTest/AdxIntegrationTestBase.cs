using DeltaKustoFileIntegrationTest;
using DeltaKustoIntegration.Database;
using DeltaKustoIntegration.Kusto;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoIntegration.TokenProvider;
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
    [Collection("ADX collection")]
    public abstract class AdxIntegrationTestBase : IntegrationTestBase
    {
        private readonly bool _overrideLoginTokenProvider;

        protected AdxIntegrationTestBase(
            AdxDbFixture adxDbFixture,
            bool overrideLoginTokenProvider = true)
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

            AdxDbFixture = adxDbFixture;
            _overrideLoginTokenProvider = overrideLoginTokenProvider;
            ClusterUri = new Uri(clusterUri);
            TenantId = tenantId;
            ServicePrincipalId = servicePrincipalId;
            ServicePrincipalSecret = servicePrincipalSecret;
        }

        protected Uri ClusterUri { get; }

        protected AdxDbFixture AdxDbFixture { get; }

        protected string TenantId { get; }

        protected string ServicePrincipalId { get; }

        protected string ServicePrincipalSecret { get; }

        protected async Task<string> InitializeDbAsync()
        {
            var dbName = await AdxDbFixture.GetCleanDbAsync();
            var gateway = CreateKustoManagementGateway(dbName);

            //  Ensures the database creation has propagated to Kusto
            while (!(await gateway.DoesDatabaseExistsAsync()))
            {
                await Task.Delay(TimeSpan.FromSeconds(.2));
            }

            return dbName;
        }

        protected async Task TestAdxToFile(string statesFolderPath)
        {
            await LoopThroughStateFilesAsync(
                Path.Combine(statesFolderPath, "States"),
                async (fromFile, toFile) =>
                {
                    var currentDbName = await InitializeDbAsync();

                    await PrepareDbAsync(fromFile, currentDbName);

                    var outputPath = Path.Combine("outputs", statesFolderPath, "adx-to-file/")
                        + Path.GetFileNameWithoutExtension(fromFile)
                        + "_2_"
                        + Path.GetFileNameWithoutExtension(toFile)
                        + ".kql";
                    var overrides = ImmutableArray<(string path, string value)>
                    .Empty
                    .Add(("jobs.main.current.adx.clusterUri", ClusterUri.ToString()))
                    .Add(("jobs.main.current.adx.database", currentDbName))
                    .Add(("jobs.main.target.scripts[0].filePath", toFile))
                    .Add(("jobs.main.action.filePath", outputPath));
                    var parameters = await RunParametersAsync(
                        "adx-to-file-params.json",
                        overrides);
                    var outputCommands = await LoadScriptAsync("", outputPath);
                    var targetCommands = CommandBase.FromScript(
                        await File.ReadAllTextAsync(toFile));

                    await ApplyCommandsAsync(outputCommands, currentDbName);

                    var finalCommands = await FetchDbCommandsAsync(currentDbName);
                    var targetModel = DatabaseModel.FromCommands(targetCommands);
                    var finalModel = DatabaseModel.FromCommands(finalCommands);
                    var finalScript = string.Join(";\n\n", finalCommands.Select(c => c.ToScript()));
                    var targetScript = string.Join(";\n\n", targetCommands.Select(c => c.ToScript()));

                    Assert.True(
                        finalModel.Equals(targetModel),
                        $"From {fromFile} to {toFile}:\n\n{finalScript}\nvs\n\n{targetScript}");
                    AdxDbFixture.ReleaseDb(currentDbName);
                });
        }

        protected async Task TestFileToAdx(string statesFolderPath)
        {
            await LoopThroughStateFilesAsync(
                Path.Combine(statesFolderPath, "States"),
                async (fromFile, toFile) =>
                {
                    var testDbName = await InitializeDbAsync();
                    var targetDbName = await InitializeDbAsync();

                    await PrepareDbAsync(toFile, targetDbName);

                    var outputPath = Path.Combine("outputs", statesFolderPath, "file-to-adx/")
                        + Path.GetFileNameWithoutExtension(fromFile)
                        + "_2_"
                        + Path.GetFileNameWithoutExtension(toFile)
                        + ".kql";
                    var overrides = ImmutableArray<(string path, string value)>
                        .Empty
                        .Add(("jobs.main.target.adx.clusterUri", ClusterUri.ToString()))
                        .Add(("jobs.main.target.adx.database", targetDbName))
                        .Append(("jobs.main.current.scripts[0].filePath", fromFile))
                        .Append(("jobs.main.action.filePath", outputPath));
                    var parameters = await RunParametersAsync(
                        "file-to-adx-params.json",
                        overrides);
                    var outputCommands = await LoadScriptAsync("", outputPath);
                    var currentCommands = CommandBase.FromScript(
                        await File.ReadAllTextAsync(fromFile));
                    var targetCommands = CommandBase.FromScript(
                        await File.ReadAllTextAsync(toFile));

                    await ApplyCommandsAsync(currentCommands.Concat(outputCommands), testDbName);

                    var finalCommands = await FetchDbCommandsAsync(testDbName);
                    var targetModel = DatabaseModel.FromCommands(targetCommands);
                    var finalModel = DatabaseModel.FromCommands(finalCommands);
                    var finalScript = string.Join(";\n\n", finalCommands.Select(c => c.ToScript()));
                    var targetScript = string.Join(";\n\n", targetCommands.Select(c => c.ToScript()));

                    Assert.True(
                        finalModel.Equals(targetModel),
                        $"From {fromFile} to {toFile}:\n\n{finalScript}\nvs\n\n{targetScript}");
                    AdxDbFixture.ReleaseDb(targetDbName);
                    AdxDbFixture.ReleaseDb(testDbName);
                });
        }

        protected async Task TestAdxToAdx(string statesFolderPath)
        {
            await LoopThroughStateFilesAsync(
                Path.Combine(statesFolderPath, "States"),
                async (fromFile, toFile) =>
                {
                    var currentDbName = await InitializeDbAsync();
                    var targetDbName = await InitializeDbAsync();

                    await Task.WhenAll(
                        PrepareDbAsync(fromFile, currentDbName),
                        PrepareDbAsync(toFile, targetDbName));

                    var outputPath = Path.Combine("outputs", statesFolderPath, "adx-to-adx/")
                        + Path.GetFileNameWithoutExtension(fromFile)
                        + "_2_"
                        + Path.GetFileNameWithoutExtension(toFile)
                        + ".kql";
                    var overrides = ImmutableArray<(string path, string value)>
                    .Empty
                    .Add(("jobs.main.current.adx.clusterUri", ClusterUri.ToString()))
                    .Add(("jobs.main.current.adx.database", currentDbName))
                    .Add(("jobs.main.target.adx.clusterUri", ClusterUri.ToString()))
                    .Add(("jobs.main.target.adx.database", targetDbName))
                    .Add(("jobs.main.action.filePath", outputPath));
                    var parameters = await RunParametersAsync(
                        "adx-to-adx-params.json",
                        overrides);
                    var targetCommands = CommandBase.FromScript(
                        await File.ReadAllTextAsync(toFile));
                    var finalCommands = await FetchDbCommandsAsync(currentDbName);
                    var targetModel = DatabaseModel.FromCommands(targetCommands);
                    var finalModel = DatabaseModel.FromCommands(finalCommands);
                    var finalScript = string.Join(";\n\n", finalCommands.Select(c => c.ToScript()));
                    var targetScript = string.Join(";\n\n", targetCommands.Select(c => c.ToScript()));

                    Assert.True(
                        targetModel.Equals(finalModel),
                        $"From {fromFile} to {toFile}:\n\n{finalScript}\nvs\n\n{targetScript}");
                    AdxDbFixture.ReleaseDb(currentDbName);
                    AdxDbFixture.ReleaseDb(targetDbName);
                });
        }

        private async Task LoopThroughStateFilesAsync(
            string folderPath,
            Func<string, string, Task> loopFunction)
        {
            var stateFiles = Directory.GetFiles(folderPath);

            Console.WriteLine($"State files:  [{string.Join(", ", stateFiles)}]");

            foreach (var fromFile in stateFiles)
            {
                foreach (var toFile in stateFiles)
                {
                    Console.WriteLine($"Current loop:  ({fromFile}, {toFile})");
                    await loopFunction(fromFile, toFile);
                }
            }
        }

        private async Task ApplyCommandsAsync(
            IEnumerable<CommandBase> commands,
            string dbName)
        {
            var gateway = CreateKustoManagementGateway(dbName);

            //  Apply commands to the db
            await gateway.ExecuteCommandsAsync(commands);
        }

        private async Task<IImmutableList<CommandBase>> FetchDbCommandsAsync(string dbName)
        {
            var gateway = CreateKustoManagementGateway(dbName);
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
            var adjustedOverrides = overrides != null
                ? overrides
                : ImmutableList<(string path, string value)>.Empty;

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
                var gateway = CreateKustoManagementGateway(dbName);

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

        private IKustoManagementGateway CreateKustoManagementGateway(string dbName)
        {
            var gateway = GatewayFactory.CreateGateway(
                ClusterUri,
                dbName,
                CreateTokenProvider());

            return gateway;
        }

        private ITokenProvider CreateTokenProvider()
        {
            var tokenProvider = TokenProviderFactory.CreateProvider(
                new TokenProviderParameterization
                {
                    Login = new ServicePrincipalLoginParameterization
                    {
                        TenantId = TenantId,
                        ClientId = ServicePrincipalId,
                        Secret = ServicePrincipalSecret
                    }
                });

            return tokenProvider!;
        }
    }
}