using DeltaKustoIntegration;
using DeltaKustoIntegration.Kusto;
using DeltaKustoIntegration.Parameterization;
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
        private readonly ConcurrentQueue<Task<string>> _preparingDbs =
            new ConcurrentQueue<Task<string>>();
        private volatile int _dbCount = 0;

        public static AdxDbTestHelper Instance { get; } = CreateSingleton();

        private static AdxDbTestHelper CreateSingleton()
        {
            var dbPrefix = Environment.GetEnvironmentVariable("deltaKustoDbPrefix");
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
            if (string.IsNullOrWhiteSpace(dbPrefix))
            {
                throw new ArgumentNullException(nameof(dbPrefix));
            }

            var tracer = new ConsoleTracer(false);
            var httpClientFactory = new SimpleHttpClientFactory(tracer);
            var tokenParameterization = new TokenProviderParameterization
            {
                Login = new ServicePrincipalLoginParameterization
                {
                    TenantId = tenantId,
                    ClientId = servicePrincipalId,
                    Secret = servicePrincipalSecret
                }
            };
            Func<string, IKustoManagementGateway> kustoManagementGatewayFactory = (db) =>
            {
                return new KustoManagementGateway(
                    new Uri(clusterUri),
                    db,
                    tokenParameterization,
                    tracer);
            };
            var helper = new AdxDbTestHelper(dbPrefix, kustoManagementGatewayFactory);

            return helper;
        }

        private AdxDbTestHelper(
            string dbPrefix,
            Func<string, IKustoManagementGateway> kustoManagementGatewayFactory)
        {
            _dbPrefix = dbPrefix;
            _kustoManagementGatewayFactory = kustoManagementGatewayFactory;
        }

        public async Task<string> GetCleanDbAsync()
        {
            Task<string>? preparingDbTask = null;

            if (_preparingDbs.TryDequeue(out preparingDbTask))
            {
                var dbName = await preparingDbTask;

                return dbName;
            }
            else
            {   //  No more database available in queue, let's take the next one
                var dbNumber = Interlocked.Increment(ref _dbCount);
                var dbName = DbNumberToDbName(dbNumber);

                await CleanDbAsync(dbName);

                return dbName;
            }
        }

        public void ReleaseDb(string dbName)
        {
            if (!dbName.StartsWith(_dbPrefix))
            {
                throw new ArgumentException("Wrong prefix", nameof(dbName));
            }

            Func<Task<string>> prepereDbAsync = async () =>
            {
                await CleanDbAsync(dbName);

                return dbName;
            };

            _preparingDbs.Enqueue(prepereDbAsync());
        }

        void IDisposable.Dispose()
        {
            //  First clean up the queue
            Task.WhenAll(_preparingDbs.ToArray()).Wait();
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