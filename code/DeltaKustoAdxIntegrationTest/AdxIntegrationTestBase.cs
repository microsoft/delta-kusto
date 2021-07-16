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
    public abstract class AdxIntegrationTestBase
        : IntegrationTestBase,
        IClassFixture<AdxDbFixture>,
        IAsyncLifetime
    {
        private readonly AdxDbFixture _adxDbFixture;
        private readonly Uri _clusterUri;
        private readonly string _currentDb;
        private readonly string _targetDb;
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

            _adxDbFixture = adxDbFixture;
            _overrideLoginTokenProvider = overrideLoginTokenProvider;
            _clusterUri = new Uri(clusterUri);
            _currentDb = adxDbFixture.GetDbName();
            _targetDb = adxDbFixture.GetDbName();
            TenantId = tenantId;
            ServicePrincipalId = servicePrincipalId;
            ServicePrincipalSecret = servicePrincipalSecret;
            CurrentDbOverrides = ImmutableArray<(string path, string value)>
                .Empty
                .Add(("jobs.main.current.adx.clusterUri", _clusterUri.ToString()))
                .Add(("jobs.main.current.adx.database", _currentDb));
            TargetDbOverrides = ImmutableArray<(string path, string value)>
                .Empty
                .Add(("jobs.main.target.adx.clusterUri", _clusterUri.ToString()))
                .Add(("jobs.main.target.adx.database", _targetDb));
        }

        async Task IAsyncLifetime.InitializeAsync()
        {
            await Task.WhenAll(
                _adxDbFixture.InitializeDbAsync(_currentDb),
                _adxDbFixture.InitializeDbAsync(_targetDb));
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            return Task.CompletedTask;
        }

        protected IEnumerable<(string path, string value)> CurrentDbOverrides { get; }

        protected IEnumerable<(string path, string value)> TargetDbOverrides { get; }

        protected string TenantId { get; }

        protected string ServicePrincipalId { get; }

        protected string ServicePrincipalSecret { get; }

        protected async Task TestAdxToFile(string statesFolderPath)
        {
            await LoopThroughStateFilesAsync(
                Path.Combine(statesFolderPath, "States"),
                async (fromFile, toFile) =>
                {
                    await PrepareDbAsync(fromFile, true);

                    var outputPath = Path.Combine("outputs", statesFolderPath, "adx-to-file/")
                        + Path.GetFileNameWithoutExtension(fromFile)
                        + "_2_"
                        + Path.GetFileNameWithoutExtension(toFile)
                        + ".kql";
                    var overrides = CurrentDbOverrides
                        .Append(("jobs.main.target.scripts[0].filePath", toFile))
                        .Append(("jobs.main.action.filePath", outputPath));
                    var parameters = await RunParametersAsync(
                        "adx-to-file-params.json",
                        overrides);
                    var outputCommands = await LoadScriptAsync("", outputPath);
                    var targetCommands = CommandBase.FromScript(
                        await File.ReadAllTextAsync(toFile));

                    await ApplyCommandsAsync(outputCommands, true);

                    var finalCommands = await FetchDbCommandsAsync(true);
                    var targetModel = DatabaseModel.FromCommands(targetCommands);
                    var finalModel = DatabaseModel.FromCommands(finalCommands);
                    var finalScript = string.Join(";\n\n", finalCommands.Select(c => c.ToScript()));
                    var targetScript = string.Join(";\n\n", targetCommands.Select(c => c.ToScript()));

                    Assert.True(
                        finalModel.Equals(targetModel),
                        $"From {fromFile} to {toFile}:\n\n{finalScript}\nvs\n\n{targetScript}");
                });
        }

        protected async Task TestFileToAdx(string statesFolderPath)
        {
            await LoopThroughStateFilesAsync(
                Path.Combine(statesFolderPath, "States"),
                async (fromFile, toFile) =>
                {
                    await PrepareDbAsync(toFile, false);

                    var outputPath = Path.Combine("outputs", statesFolderPath, "file-to-adx/")
                        + Path.GetFileNameWithoutExtension(fromFile)
                        + "_2_"
                        + Path.GetFileNameWithoutExtension(toFile)
                        + ".kql";
                    var overrides = TargetDbOverrides
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

                    await ApplyCommandsAsync(
                        currentCommands.Concat(outputCommands),
                        true);

                    var finalCommands = await FetchDbCommandsAsync(true);
                    var targetModel = DatabaseModel.FromCommands(targetCommands);
                    var finalModel = DatabaseModel.FromCommands(finalCommands);
                    var finalScript = string.Join(";\n\n", finalCommands.Select(c => c.ToScript()));
                    var targetScript = string.Join(";\n\n", targetCommands.Select(c => c.ToScript()));

                    Assert.True(
                        finalModel.Equals(targetModel),
                        $"From {fromFile} to {toFile}:\n\n{finalScript}\nvs\n\n{targetScript}");
                });
        }

        protected async Task TestAdxToAdx(string statesFolderPath)
        {
            await LoopThroughStateFilesAsync(
                Path.Combine(statesFolderPath, "States"),
                async (fromFile, toFile) =>
                {
                    await Task.WhenAll(
                        PrepareDbAsync(fromFile, true),
                        PrepareDbAsync(toFile, false));

                    var outputPath = Path.Combine("outputs", statesFolderPath, "adx-to-adx/")
                        + Path.GetFileNameWithoutExtension(fromFile)
                        + "_2_"
                        + Path.GetFileNameWithoutExtension(toFile)
                        + ".kql";
                    var overrides = CurrentDbOverrides
                        .Concat(TargetDbOverrides)
                        .Append(("jobs.main.action.filePath", outputPath));
                    var parameters = await RunParametersAsync(
                        "adx-to-adx-params.json",
                        overrides);
                    var targetCommands = CommandBase.FromScript(
                        await File.ReadAllTextAsync(toFile));
                    var finalCommands = await FetchDbCommandsAsync(false);
                    var targetModel = DatabaseModel.FromCommands(targetCommands);
                    var finalModel = DatabaseModel.FromCommands(finalCommands);
                    var finalScript = string.Join(";\n\n", finalCommands.Select(c => c.ToScript()));
                    var targetScript = string.Join(";\n\n", targetCommands.Select(c => c.ToScript()));

                    Assert.True(
                        targetModel.Equals(finalModel),
                        $"From {fromFile} to {toFile}:\n\n{finalScript}\nvs\n\n{targetScript}");
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
                    await CleanDatabasesAsync();
                    await loopFunction(fromFile, toFile);
                }
            }
        }

        private async Task ApplyCommandsAsync(
            IEnumerable<CommandBase> commands,
            bool isCurrent)
        {
            var gateway = CreateKustoManagementGateway(isCurrent);

            //  Apply commands to the db
            await gateway.ExecuteCommandsAsync(commands);
        }

        private async Task<IImmutableList<CommandBase>> FetchDbCommandsAsync(bool isCurrent)
        {
            var gateway = CreateKustoManagementGateway(isCurrent);
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

        protected async Task CleanDatabasesAsync()
        {
            await Task.WhenAll(
                CleanDbAsync(true),
                CleanDbAsync(false));
        }

        protected async Task PrepareDbAsync(
            string scriptPath,
            bool isCurrent)
        {
            var script = await File.ReadAllTextAsync(scriptPath);

            try
            {
                var commands = CommandBase.FromScript(script);
                var gateway = CreateKustoManagementGateway(isCurrent);

                await gateway.ExecuteCommandsAsync(commands);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failure during PrepareDb.  isCurrent={isCurrent}.  "
                    + $"Script path = '{scriptPath}'.  "
                    + $"Script = '{script.Replace("\n", "\\n").Replace("\r", "\\r")}'",
                    ex);
            }
        }

        private IKustoManagementGateway CreateKustoManagementGateway(bool isCurrent)
        {
            var gateway = GatewayFactory.CreateGateway(
                _clusterUri,
                isCurrent ? _currentDb : _targetDb,
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

        private async Task CleanDbAsync(bool isCurrent)
        {
            var emptyDbProvider = (IDatabaseProvider)new EmptyDatabaseProvider();
            var kustoGateway = CreateKustoManagementGateway(isCurrent);
            var dbProvider = (IDatabaseProvider)new KustoDatabaseProvider(
                new ConsoleTracer(false),
                kustoGateway);
            var emptyDb = await emptyDbProvider.RetrieveDatabaseAsync();
            var db = await dbProvider.RetrieveDatabaseAsync();
            var currentDeltaCommands = db.ComputeDelta(emptyDb);

            await kustoGateway.ExecuteCommandsAsync(currentDeltaCommands);
        }
    }
}