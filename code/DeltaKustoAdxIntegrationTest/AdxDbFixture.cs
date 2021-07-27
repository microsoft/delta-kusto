using DeltaKustoIntegration;
using DeltaKustoIntegration.Kusto;
using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoAdxIntegrationTest
{
    public class AdxDbFixture : IDisposable
    {
        private const int AHEAD_PROVISIONING_COUNT = 20;

        private readonly Lazy<string> _dbPrefix;
        private readonly Lazy<Func<string, IKustoManagementGateway>> _kustoManagementGatewayFactory;
        private readonly Lazy<AzureManagementGateway> _azureManagementGateway;
        private readonly Lazy<Task> _initializedAsync;
        private readonly ConcurrentStack<string> _existingDbs = new ConcurrentStack<string>();
        private volatile int _dbCount = 0;

        public AdxDbFixture()
        {
            //  Environment variables aren't available until another fixture is executed
            //  To avoid conflict, we simply lazy load every thing so that loading environment
            //  Variables is triggered by tests hence after all fixtures have run
            _dbPrefix = new Lazy<string>(
                () =>
                {
                    var dbPrefix = Environment.GetEnvironmentVariable("deltaKustoDbPrefix");

                    if (string.IsNullOrWhiteSpace(dbPrefix))
                    {
                        throw new ArgumentNullException(nameof(dbPrefix));
                    }

                    return dbPrefix;
                },
                true);
            _kustoManagementGatewayFactory = new Lazy<Func<string, IKustoManagementGateway>>(
                () =>
                {
                    var clusterUri = Environment.GetEnvironmentVariable("deltaKustoClusterUri");
                    var tenantId = Environment.GetEnvironmentVariable("deltaKustoTenantId");
                    var servicePrincipalId = Environment.GetEnvironmentVariable("deltaKustoSpId");
                    var servicePrincipalSecret = Environment.GetEnvironmentVariable("deltaKustoSpSecret");

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
                    if (string.IsNullOrWhiteSpace(clusterUri))
                    {
                        throw new ArgumentNullException(nameof(clusterUri));
                    }

                    var tracer = new ConsoleTracer(false);
                    var httpClientFactory = new SimpleHttpClientFactory(tracer);
                    var tokenProvider = new LoginTokenProvider(
                        tracer,
                        httpClientFactory,
                        tenantId,
                        servicePrincipalId,
                        servicePrincipalSecret);
                    Func<string, IKustoManagementGateway> factory = (db) =>
                    {
                        return new KustoManagementGateway(
                            new Uri(clusterUri),
                            db,
                            tokenProvider,
                            tracer,
                            httpClientFactory);
                    };

                    return factory;
                },
                true);
            _azureManagementGateway = new Lazy<AzureManagementGateway>(
                () =>
                {
                    var clusterId = Environment.GetEnvironmentVariable("deltaKustoClusterId");
                    var tenantId = Environment.GetEnvironmentVariable("deltaKustoTenantId");
                    var servicePrincipalId = Environment.GetEnvironmentVariable("deltaKustoSpId");
                    var servicePrincipalSecret = Environment.GetEnvironmentVariable("deltaKustoSpSecret");

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
                    if (string.IsNullOrWhiteSpace(clusterId))
                    {
                        throw new ArgumentNullException(nameof(clusterId));
                    }

                    var tracer = new ConsoleTracer(false);
                    var httpClientFactory = new SimpleHttpClientFactory(tracer);
                    var tokenProvider = new LoginTokenProvider(
                        tracer,
                        httpClientFactory,
                        tenantId,
                        servicePrincipalId,
                        servicePrincipalSecret);

                    return new AzureManagementGateway(
                        clusterId,
                        tokenProvider,
                        tracer,
                        httpClientFactory);
                },
                true);
            _initializedAsync = new Lazy<Task>(
                async () =>
                {
                    await CleanDbsAsync();
                },
                true);
        }

        public async Task<string> GetCleanDbAsync()
        {
            await _initializedAsync.Value;

            string? dbName;

            if (_existingDbs.TryPop(out dbName) && dbName != null)
            {   //  Clean existing database
                var kustoGateway = _kustoManagementGatewayFactory.Value(dbName);
                var currentCommands = await kustoGateway.ReverseEngineerDatabaseAsync();
                var currentModel = DatabaseModel.FromCommands(currentCommands);
                var cleanCommands = currentModel.ComputeDelta(
                    DatabaseModel.FromCommands(
                        ImmutableArray<CommandBase>.Empty));

                await kustoGateway.ExecuteCommandsAsync(cleanCommands);

                return dbName;
            }
            else
            {
                dbName = DbNumberToDbName(Interlocked.Increment(ref _dbCount));
                await _azureManagementGateway.Value.CreateDatabaseAsync(dbName);

                var kustoGateway = _kustoManagementGatewayFactory.Value(dbName);

                while (!(await kustoGateway.DoesDatabaseExistsAsync()))
                {
                    await Task.Delay(TimeSpan.FromSeconds(.2));
                }

                return dbName;
            }
        }

        public void ReleaseDb(string dbName)
        {
            if (!dbName.StartsWith(_dbPrefix.Value))
            {
                throw new ArgumentException("Wrong prefix", nameof(dbName));
            }

            _existingDbs.Push(dbName);
        }

        void IDisposable.Dispose()
        {
            CleanDbsAsync().Wait();
        }

        private async Task CleanDbsAsync()
        {
            var names = await _azureManagementGateway.Value.GetDatabaseNamesAsync();
            var deleteTasks = names
                .Where(n => n.StartsWith(_dbPrefix.Value))
                .Select(n => _azureManagementGateway.Value.DeleteDatabaseAsync(n));

            await Task.WhenAll(deleteTasks);
        }

        private string DbNumberToDbName(int c)
        {
            return $"{_dbPrefix.Value}{c.ToString("D8")}";
        }
    }
}