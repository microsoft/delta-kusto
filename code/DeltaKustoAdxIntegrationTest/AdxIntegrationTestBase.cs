using DeltaKustoFileIntegrationTest;
using DeltaKustoIntegration.Database;
using DeltaKustoIntegration.Kusto;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoAdxIntegrationTest
{
    public abstract class AdxIntegrationTestBase : IntegrationTestBase
    {
        private static readonly TimeSpan TIME_OUT = TimeSpan.FromSeconds(90);

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
            CurrentDbOverrides = ImmutableArray<(string path, string value)>
                .Empty
                .Add(("jobs.main.current.adx.clusterUri", _clusterUri.ToString()))
                .Add(("jobs.main.current.adx.database", _currentDb));
            TargetDbOverrides = ImmutableArray<(string path, string value)>
                .Empty
                .Add(("jobs.main.target.adx.clusterUri", _clusterUri.ToString()))
                .Add(("jobs.main.target.adx.database", _targetDb));
        }

        protected IEnumerable<(string path, string value)> CurrentDbOverrides { get; }

        protected IEnumerable<(string path, string value)> TargetDbOverrides { get; }

        protected async Task TestAdxToFile(string statesFolderPath, string outputFolder)
        {
            var cancellationToken = new CancellationTokenSource(TIME_OUT);
            var ct = cancellationToken.Token;

            await LoopThroughStateFilesAsync(
                statesFolderPath,
                ct,
                async (fromFile, toFile) =>
                {
                    await PrepareDbAsync(fromFile, true, ct);

                    var outputPath = outputFolder
                        + Path.GetFileNameWithoutExtension(fromFile)
                        + "_2_"
                        + Path.GetFileNameWithoutExtension(toFile)
                        + ".kql";
                    var overrides = CurrentDbOverrides
                        .Append(("jobs.main.target.scripts[0].filePath", toFile))
                        .Append(("jobs.main.action.filePath", outputPath));
                    var parameters = await RunParametersAsync(
                        "adx-to-file-params.json",
                        ct,
                        overrides);
                    var outputCommands = await LoadScriptAsync(outputPath);
                    var targetCommands = CommandBase.FromScript(
                        await File.ReadAllTextAsync(toFile));

                    await ApplyCommandsAsync(outputCommands, true, ct);

                    var finalCommands = await FetchDbCommandsAsync(true, ct);
                    var finalScript = string.Join("\n", finalCommands.Select(c => c.ToScript()));
                    var targetScript = string.Join("\n", targetCommands.Select(c => c.ToScript()));

                    Assert.True(
                        finalCommands.SequenceEqual(targetCommands),
                        $"From {fromFile} to {toFile}:\n\n{finalScript}\nvs\n\n{targetScript}");
                });
        }

        protected async Task TestFileToAdx(string statesFolderPath, string outputFolder)
        {
            var cancellationToken = new CancellationTokenSource(TIME_OUT);
            var ct = cancellationToken.Token;

            await LoopThroughStateFilesAsync(
                statesFolderPath,
                ct,
                async (fromFile, toFile) =>
                {
                    await PrepareDbAsync(toFile, false, ct);

                    var outputPath = outputFolder
                        + Path.GetFileNameWithoutExtension(fromFile)
                        + "_2_"
                        + Path.GetFileNameWithoutExtension(toFile)
                        + ".kql";
                    var overrides = TargetDbOverrides
                        .Append(("jobs.main.current.scripts[0].filePath", fromFile))
                        .Append(("jobs.main.action.filePath", outputPath));
                    var parameters = await RunParametersAsync(
                        "file-to-adx-params.json",
                        ct,
                        overrides);
                    var outputCommands = await LoadScriptAsync(outputPath);
                    var currentCommands = CommandBase.FromScript(
                        await File.ReadAllTextAsync(fromFile));
                    var targetCommands = CommandBase.FromScript(
                        await File.ReadAllTextAsync(toFile));

                    await ApplyCommandsAsync(
                        currentCommands.Concat(outputCommands),
                        true,
                        ct);

                    var finalCommands = await FetchDbCommandsAsync(true, ct);
                    var finalScript = string.Join("\n", finalCommands.Select(c => c.ToScript()));
                    var targetScript = string.Join("\n", targetCommands.Select(c => c.ToScript()));

                    Assert.True(
                        finalCommands.SequenceEqual(targetCommands),
                        $"From {fromFile} to {toFile}:\n\n{finalScript}\nvs\n\n{targetScript}");
                });
        }

        protected async Task TestAdxToAdx(string statesFolderPath, string outputFolder)
        {
            var cancellationToken = new CancellationTokenSource(TIME_OUT);
            var ct = cancellationToken.Token;

            await LoopThroughStateFilesAsync(
                statesFolderPath,
                ct,
                async (fromFile, toFile) =>
                {
                    await Task.WhenAll(
                        PrepareDbAsync(fromFile, true, ct),
                        PrepareDbAsync(toFile, false, ct));

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
                        ct,
                        overrides);
                    var targetCommands = CommandBase.FromScript(
                        await File.ReadAllTextAsync(toFile));
                    var finalCommands = await FetchDbCommandsAsync(false, ct);
                    var finalScript = string.Join("\n", finalCommands.Select(c => c.ToScript()));
                    var targetScript = string.Join("\n", targetCommands.Select(c => c.ToScript()));

                    Assert.True(
                        finalCommands.SequenceEqual(targetCommands),
                        $"From {fromFile} to {toFile}:\n\n{finalScript}\nvs\n\n{targetScript}");
                });
        }

        private async Task LoopThroughStateFilesAsync(
            string folderPath,
            CancellationToken ct,
            Func<string, string, Task> loopFunction)
        {
            var stateFiles = Directory.GetFiles(folderPath);

            Console.WriteLine($"State files:  [{string.Join(", ", stateFiles)}]");

            foreach (var fromFile in stateFiles)
            {
                foreach (var toFile in stateFiles)
                {
                    Console.WriteLine($"Current loop:  ({fromFile}, {toFile})");
                    await CleanDatabasesAsync(ct);
                    await loopFunction(fromFile, toFile);
                }
            }
        }

        private async Task ApplyCommandsAsync(
            IEnumerable<CommandBase> commands,
            bool isCurrent,
            CancellationToken ct)
        {
            var gateway = CreateKustoManagementGateway(isCurrent);

            //  Apply commands to the db
            await gateway.ExecuteCommandsAsync(commands, ct);
        }

        private async Task<IImmutableList<CommandBase>> FetchDbCommandsAsync(
            bool isCurrent,
            CancellationToken ct)
        {
            var gateway = CreateKustoManagementGateway(isCurrent);
            var dbProvider = (IDatabaseProvider)new KustoDatabaseProvider(
                new ConsoleTracer(false),
                gateway);
            var emptyProvider = (IDatabaseProvider)new EmptyDatabaseProvider();
            var finalDb = await dbProvider.RetrieveDatabaseAsync(ct);
            var emptyDb = await emptyProvider.RetrieveDatabaseAsync(ct);
            //  Use the delta from an empty db to get 
            var finalCommands = emptyDb.ComputeDelta(finalDb);

            return finalCommands;
        }

        protected override Task<MainParameterization> RunParametersAsync(
            string parameterFilePath,
            CancellationToken ct,
            IEnumerable<(string path, string value)>? overrides = null)
        {
            var adjustedOverrides = overrides != null
                ? overrides
                : ImmutableList<(string path, string value)>.Empty;

            adjustedOverrides = adjustedOverrides
                .Append(("tokenProvider.login.tenantId", _tenantId))
                .Append(("tokenProvider.login.clientId", _servicePrincipalId))
                .Append(("tokenProvider.login.secret", _servicePrincipalSecret));

            return base.RunParametersAsync(parameterFilePath, ct, adjustedOverrides);
        }

        protected async Task CleanDatabasesAsync(CancellationToken ct)
        {
            await Task.WhenAll(
                CleanDbAsync(true, ct),
                CleanDbAsync(false, ct));
        }

        protected async Task PrepareDbAsync(
            string scriptPath,
            bool isCurrent,
            CancellationToken ct)
        {
            var script = await File.ReadAllTextAsync(scriptPath);

            try
            {
                var commands = CommandBase.FromScript(script);
                var gateway = CreateKustoManagementGateway(isCurrent);

                await gateway.ExecuteCommandsAsync(commands, ct);
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
                        TenantId = _tenantId,
                        ClientId = _servicePrincipalId,
                        Secret = _servicePrincipalSecret
                    }
                });

            return tokenProvider!;
        }

        private async Task CleanDbAsync(bool isCurrent, CancellationToken ct)
        {
            var emptyDbProvider = (IDatabaseProvider)new EmptyDatabaseProvider();
            var kustoGateway = CreateKustoManagementGateway(isCurrent);
            var dbProvider = (IDatabaseProvider)new KustoDatabaseProvider(
                new ConsoleTracer(false),
                kustoGateway);
            var emptyDb = await emptyDbProvider.RetrieveDatabaseAsync(ct);
            var db = await dbProvider.RetrieveDatabaseAsync(ct);
            var currentDeltaCommands = db.ComputeDelta(emptyDb);

            await kustoGateway.ExecuteCommandsAsync(currentDeltaCommands, ct);
        }
    }
}