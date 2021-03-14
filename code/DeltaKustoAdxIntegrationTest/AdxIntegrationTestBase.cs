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
                .Add(("jobs.main.current.database.clusterUri", _clusterUri))
                .Add(("jobs.main.current.database.database", _currentDb));
            TargetDbOverrides = ImmutableArray<(string path, object value)>
                .Empty
                .Add(("jobs.main.target.database.clusterUri", (object)_clusterUri))
                .Add(("jobs.main.target.database.database", _currentDb));
        }

        protected IEnumerable<(string path, object value)> CurrentDbOverrides { get; }

        protected IEnumerable<(string path, object value)> TargetDbOverrides { get; }

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

        protected async Task PrepareDbAsync(string scriptPath, bool isCurrent)
        {
            var script = await File.ReadAllTextAsync(scriptPath);
            var commands = CommandBase.FromScript(script);
            var gateway = CreateKustoManagementGateway(isCurrent);

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

        protected async Task CleanDatabasesAsync()
        {
            await Task.WhenAll(
                CleanDbAsync(true),
                CleanDbAsync(false));
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