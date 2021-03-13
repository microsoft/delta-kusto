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
        private readonly bool _overrideCurrentDb;
        private readonly bool _overrideTargetDb;
        private readonly Uri _clusterUri;
        private readonly string _currentDb;
        private readonly string _targetDb;
        private readonly string _tenantId;
        private readonly string _servicePrincipalId;
        private readonly string _servicePrincipalSecret;
        private bool _isClean = false;

        protected AdxIntegrationTestBase(bool overrideCurrentDb, bool overrideTargetDb)
        {
            _overrideCurrentDb = overrideCurrentDb;
            _overrideTargetDb = overrideTargetDb;

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
        }

        protected override async Task<int> RunMainAsync(params string[] args)
        {
            await EnsureCleanAsync();

            return await base.RunMainAsync(args);
        }

        protected override Task<MainParameterization> RunParametersAsync(
            string parameterFilePath,
            (string path, object value)[]? overrides = null)
        {
            var adjustedOverrides = overrides != null
                ? overrides.ToImmutableList()
                : ImmutableList<(string path, object value)>.Empty;

            adjustedOverrides = adjustedOverrides.Add(
                ("tokenProvider.login.tenantId", _tenantId));
            adjustedOverrides = adjustedOverrides.Add(
                ("tokenProvider.login.clientId", _servicePrincipalId));
            adjustedOverrides = adjustedOverrides.Add(
                ("tokenProvider.login.secret", _servicePrincipalSecret));

            if (_overrideCurrentDb)
            {
                adjustedOverrides = adjustedOverrides.Add(
                    ("jobs.main.current.database.clusterUri", _clusterUri));
                adjustedOverrides = adjustedOverrides.Add(
                    ("jobs.main.current.database.database", _currentDb));
            }
            if (_overrideTargetDb)
            {
                adjustedOverrides = adjustedOverrides.Add(
                    ("jobs.main.target.database.clusterUri", _clusterUri));
                adjustedOverrides = adjustedOverrides.Add(
                    ("jobs.main.target.database.database", _targetDb));
            }

            return base.RunParametersAsync(parameterFilePath, adjustedOverrides.ToArray());
        }

        protected async Task PrepareCurrentAsync(string scriptPath)
        {
            var script = await File.ReadAllTextAsync(scriptPath);
            var commands = CommandBase.FromScript(script);
            var gateway = CreateKustoManagementGateway(true);

            await EnsureCleanAsync();
            await gateway.ExecuteCommandsAsync(commands);
        }

        protected IKustoManagementGateway CreateKustoManagementGateway(bool isCurrent)
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

        private async Task EnsureCleanAsync()
        {
            if (!_isClean)
            {
                await EnsureCleanDbAsync(true);
                await EnsureCleanDbAsync(false);

                _isClean = true;
            }
        }

        private async Task EnsureCleanDbAsync(bool isCurrent)
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