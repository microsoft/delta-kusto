using DeltaKustoIntegration;
using DeltaKustoIntegration.Kusto;
using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoAdxIntegrationTest
{
    public class AdxDbFixture : IDisposable
    {
        private readonly Lazy<string> _dbPrefix;
        private readonly Lazy<AzureManagementGateway> _azureManagementGateway;
        private readonly Lazy<Task> _cleanDbAsync;
        private volatile int _dbCount;

        public AdxDbFixture()
        {
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
            _cleanDbAsync = new Lazy<Task>(() => CleanDbAsync(), true);
        }

        public string GetDbName()
        {
            var dbCount = Interlocked.Increment(ref _dbCount);
            var name = $"{_dbPrefix.Value}{dbCount}";

            return name;
        }

        public async Task InitializeDbAsync(string dbName)
        {
            if (!dbName.StartsWith(_dbPrefix.Value))
            {
                throw new ArgumentException("Wrong prefix", nameof(dbName));
            }

            await _cleanDbAsync.Value;
            await _azureManagementGateway.Value.CreateDatabaseAsync(dbName);
        }

        void IDisposable.Dispose()
        {
        }

        private async Task CleanDbAsync()
        {
            var names = await _azureManagementGateway.Value.GetDatabaseNamesAsync();
            var deleteTasks = names
                .Where(n => n.StartsWith(_dbPrefix.Value))
                .Select(n => _azureManagementGateway.Value.DeleteDatabaseAsync(n));

            await Task.WhenAll(deleteTasks);
        }
    }
}