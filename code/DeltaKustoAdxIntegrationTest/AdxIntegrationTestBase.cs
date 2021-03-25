using delta_kusto;
using DeltaKustoFileIntegrationTest;
using DeltaKustoIntegration.Database;
using DeltaKustoIntegration.Kusto;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest
{
    public abstract class AdxIntegrationTestBase : IntegrationTestBase
    {
        private readonly Uri _clusterUri;
        private readonly string _currentDb;
        private readonly string _targetDb;
        private readonly string _tenantId;
        private readonly string _servicePrincipalId;
        private readonly string _servicePrincipalSecret;

        protected AdxIntegrationTestBase()
        {
            var clusterUri = Environment.GetEnvironmentVariable("deltaKustoClusterUri");
            var currentDb = Environment.GetEnvironmentVariable("deltaKustoCurrentDb");
            var targetDb = Environment.GetEnvironmentVariable("deltaKustoTargetDb");
            var tenantId = Environment.GetEnvironmentVariable("deltaKustoTenantId");
            var servicePrincipalId = Environment.GetEnvironmentVariable("deltaKustoSpId");
            var servicePrincipalSecret = Environment.GetEnvironmentVariable("deltaKustoSpSecret");

            if (string.IsNullOrWhiteSpace(clusterUri))
            {
                throw new ArgumentNullException(nameof(clusterUri));
            }
            if (string.IsNullOrWhiteSpace(currentDb))
            {
                throw new ArgumentNullException(nameof(currentDb));
            }
            if (string.IsNullOrWhiteSpace(targetDb))
            {
                throw new ArgumentNullException(nameof(targetDb));
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

            _clusterUri = new Uri(clusterUri);
            _currentDb = currentDb;
            _targetDb = targetDb;
            _tenantId = tenantId;
            _servicePrincipalId = servicePrincipalId;
            _servicePrincipalSecret = servicePrincipalSecret;
            CurrentDbOverrides = ImmutableArray<(string path, object value)>
                .Empty
                .Add(("jobs.main.current.adx.clusterUri", _clusterUri))
                .Add(("jobs.main.current.adx.database", _currentDb));
            TargetDbOverrides = ImmutableArray<(string path, object value)>
                .Empty
                .Add(("jobs.main.target.adx.clusterUri", (object)_clusterUri))
                .Add(("jobs.main.target.adx.database", _targetDb));
        }

        protected IEnumerable<(string path, object value)> CurrentDbOverrides { get; }

        protected IEnumerable<(string path, object value)> TargetDbOverrides { get; }

        protected async Task TestAdxToFile(string statesFolderPath, string outputFolder)
        {
            await LoopThroughStateFilesAsync(
                statesFolderPath,
                async (fromFile, toFile) =>
                {
                    await PrepareDbAsync(fromFile, true);

                    var outputPath = outputFolder
                        + Path.GetFileNameWithoutExtension(fromFile)
                        + "_2_"
                        + Path.GetFileNameWithoutExtension(toFile)
                        + ".kql";
                    var overrides = CurrentDbOverrides
                        .Append(("jobs.main.target.scripts", new[] { new { filePath = toFile } }))
                        .Append(("jobs.main.action.filePath", outputPath));
                    var parameters = await RunParametersAsync(
                        "adx-to-file-params.json",
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

        protected async Task TestFileToAdx(string statesFolderPath, string outputFolder)
        {
            await LoopThroughStateFilesAsync(
                statesFolderPath,
                async (fromFile, toFile) =>
                {
                    await PrepareDbAsync(toFile, false);

                    var outputPath = outputFolder
                        + Path.GetFileNameWithoutExtension(fromFile)
                        + "_2_"
                        + Path.GetFileNameWithoutExtension(toFile)
                        + ".kql";
                    var overrides = TargetDbOverrides
                        .Append(("jobs.main.current.scripts", new[] { new { filePath = fromFile } }))
                        .Append(("jobs.main.action.filePath", outputPath));
                    var parameters = await RunParametersAsync(
                        "file-to-adx-params.json",
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

        protected async Task TestAdxToAdx(string statesFolderPath, string outputFolder)
        {
            await LoopThroughStateFilesAsync(
                statesFolderPath,
                async (fromFile, toFile) =>
                {
                    await Task.WhenAll(
                        PrepareDbAsync(fromFile, true),
                        PrepareDbAsync(toFile, false));

                    var outputPath = outputFolder
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

                    Assert.True(
                        finalCommands.SequenceEqual(targetCommands),
                        $"From {fromFile} to {toFile}");
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

        protected override Task<MainParameterization> RunParametersAsync(
            string parameterFilePath,
            IEnumerable<(string path, object value)>? overrides = null)
        {
            var adjustedOverrides = overrides != null
                ? overrides
                : ImmutableList<(string path, object value)>.Empty;

            adjustedOverrides = adjustedOverrides
                .Append(("tokenProvider.login.tenantId", _tenantId))
                .Append(("tokenProvider.login.clientId", _servicePrincipalId))
                .Append(("tokenProvider.login.secret", _servicePrincipalSecret));

            return base.RunParametersAsync(parameterFilePath, adjustedOverrides);
        }

        protected async Task CleanDatabasesAsync()
        {
            await Task.WhenAll(
                CleanDbAsync(true),
                CleanDbAsync(false));
        }

        protected async Task PrepareDbAsync(string scriptPath, bool isCurrent)
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
            var gatewayFactory =
                new KustoManagementGatewayFactory() as IKustoManagementGatewayFactory;
            var gateway = gatewayFactory.CreateGateway(
                _clusterUri,
                isCurrent ? _currentDb : _targetDb,
                CreateTokenProvider());

            return gateway;
        }

        private ITokenProvider CreateTokenProvider()
        {
            var tokenProviderFactory = new TokenProviderFactory() as ITokenProviderFactory;
            var tokenProvider = tokenProviderFactory.CreateProvider(
                new TokenProviderParameterization
                {
                    Login = new ServicePrincipalLoginParameterization
                    {
                        TenantId = _tenantId,
                        ClientId = _servicePrincipalId,
                        Secret = _servicePrincipalSecret
                    }
                });

            return tokenProvider!;
        }

        private async Task CleanDbAsync(bool isCurrent)
        {
            var emptyDbProvider = (IDatabaseProvider)new EmptyDatabaseProvider();
            var kustoGateway = CreateKustoManagementGateway(isCurrent);
            var dbProvider = (IDatabaseProvider)new KustoDatabaseProvider(kustoGateway);
            var emptyDb = await emptyDbProvider.RetrieveDatabaseAsync();
            var db = await dbProvider.RetrieveDatabaseAsync();
            var currentDeltaCommands = db.ComputeDelta(emptyDb);

            await kustoGateway.ExecuteCommandsAsync(currentDeltaCommands);
        }
    }
}