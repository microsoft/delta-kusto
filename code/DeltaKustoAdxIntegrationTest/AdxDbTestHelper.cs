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
    public class AdxDbTestHelper : IDisposable
    {
        private readonly string _dbPrefix;
        private readonly Func<string, IKustoManagementGateway> _kustoManagementGatewayFactory;
        private readonly AzureManagementGateway _azureManagementGateway;
        private readonly Task _initializedAsync;
        private readonly ConcurrentQueue<Task<string>> _preparingDbs =
            new ConcurrentQueue<Task<string>>();
        private volatile int _dbCount = 0;

        public static AdxDbTestHelper Instance { get; } = CreateSingleton();

        private static AdxDbTestHelper CreateSingleton()
        {
            var dbPrefix = Environment.GetEnvironmentVariable("deltaKustoDbPrefix");
            var clusterId = Environment.GetEnvironmentVariable("deltaKustoClusterId");
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
            if (string.IsNullOrWhiteSpace(clusterId))
            {
                throw new ArgumentNullException(nameof(clusterId));
            }
            if (string.IsNullOrWhiteSpace(dbPrefix))
            {
                throw new ArgumentNullException(nameof(dbPrefix));
            }

            var tracer = new ConsoleTracer(false);
            var httpClientFactory = new SimpleHttpClientFactory(tracer);
            var tokenProvider = new LoginTokenProvider(
                tracer,
                httpClientFactory,
                tenantId,
                servicePrincipalId,
                servicePrincipalSecret);
            Func<string, IKustoManagementGateway> kustoManagementGatewayFactory = (db) =>
            {
                return new KustoManagementGateway(
                    new Uri(clusterUri),
                    db,
                    tokenProvider,
                    tracer,
                    httpClientFactory);
            };
            var azureManagementGateway = new AzureManagementGateway(
                        clusterId,
                        tokenProvider,
                        tracer,
                        httpClientFactory);
            var helper = new AdxDbTestHelper(
                dbPrefix,
                kustoManagementGatewayFactory,
                azureManagementGateway);

            return helper;
        }

        private AdxDbTestHelper(
            string dbPrefix,
            Func<string, IKustoManagementGateway> kustoManagementGatewayFactory,
            AzureManagementGateway azureManagementGateway)
        {
            _dbPrefix = dbPrefix;
            _kustoManagementGatewayFactory = kustoManagementGatewayFactory;
            _azureManagementGateway = azureManagementGateway;
            _initializedAsync = InitializeAsync();
        }

        public async Task<string> GetCleanDbAsync()
        {
            await _initializedAsync;

            Task<string>? preparingDbTask = null;

            if (_preparingDbs.TryDequeue(out preparingDbTask))
            {
                var dbName = await preparingDbTask;

                return dbName;
            }
            else
            {   //  No more database available
                throw new InvalidOperationException("No database available");
            }
        }

        public void ReleaseDb(string dbName)
        {
            if (!dbName.StartsWith(_dbPrefix))
            {
                throw new ArgumentException("Wrong prefix", nameof(dbName));
            }

            Func<Task<string>> preperaDbAsync = async () =>
            {
                await CleanDbAsync(dbName);

                return dbName;
            };

            _preparingDbs.Enqueue(preperaDbAsync());
        }

        void IDisposable.Dispose()
        {
            //  First clean up the queue
            Task.WhenAll(_preparingDbs.ToArray()).Wait();
        }

        private async Task InitializeAsync()
        {   //  Let's find the existing dbs and enqueue them as ready
            var names = await _azureManagementGateway.GetDatabaseNamesAsync();
            var usefulNames = names
                .Where(n => n.StartsWith(_dbPrefix))
                .Where(n => IsInteger(n.Substring(_dbPrefix.Length)))
                .ToImmutableArray();
            var maxCount = usefulNames
                .Select(n => n.Substring(_dbPrefix.Length))
                .Select(t => int.Parse(t))
                .Max();

            _dbCount = maxCount + 1;

            foreach (var name in usefulNames)
            {
                Func<Task<string>> prepareDbAsync = async () =>
                {
                    await CleanDbAsync(name);

                    return name;
                };

                _preparingDbs.Enqueue(prepareDbAsync());
            }
        }

        private static bool IsInteger(string text)
        {
            return int.TryParse(text, out _);
        }

        private async Task CleanDbAsync(string dbName)
        {
            var kustoGateway = _kustoManagementGatewayFactory(dbName);
            var currentCommands = await kustoGateway.ReverseEngineerDatabaseAsync();
            var currentModel = DatabaseModel.FromCommands(currentCommands);
            var cleanCommands = currentModel.ComputeDelta(
                DatabaseModel.FromCommands(
                    ImmutableArray<CommandBase>.Empty));

            await kustoGateway.ExecuteCommandsAsync(cleanCommands);
        }

        private string DbNumberToDbName(int c)
        {
            return $"{_dbPrefix}{c.ToString("D8")}";
        }
    }
}