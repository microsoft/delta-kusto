using DeltaKustoIntegration;
using DeltaKustoIntegration.Kusto;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.KustoModel;
using System;
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
        private readonly int _maxDbs;
        private readonly List<string> _availableDbNames;
        private readonly object _queueLock = new();
        private volatile TaskCompletionSource _newDbEvent = new TaskCompletionSource();

        public static AdxDbTestHelper Instance { get; } = CreateSingleton();

        #region Constructors
        private static AdxDbTestHelper CreateSingleton()
        {
            var dbPrefix = Environment.GetEnvironmentVariable("deltaKustoDbPrefix");
            var clusterUri = Environment.GetEnvironmentVariable("deltaKustoClusterUri");
            var tenantId = Environment.GetEnvironmentVariable("deltaKustoTenantId");
            var servicePrincipalId = Environment.GetEnvironmentVariable("deltaKustoSpId");
            var servicePrincipalSecret = Environment.GetEnvironmentVariable("deltaKustoSpSecret");
            var maxDbsText = Environment.GetEnvironmentVariable("maxDbs");

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
            if (string.IsNullOrWhiteSpace(maxDbsText) || !int.TryParse(maxDbsText, out _))
            {
                throw new ArgumentNullException(nameof(maxDbsText));
            }

            var maxDbs = int.Parse(maxDbsText);
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
            var kustoGatewayFactory = new KustoManagementGatewayFactory(
                tokenParameterization,
                tracer,
                null);
            var helper = new AdxDbTestHelper(
                dbPrefix,
                db => kustoGatewayFactory.CreateGateway(new Uri(clusterUri), db),
                maxDbs);

            return helper;
        }

        private AdxDbTestHelper(
            string dbPrefix,
            Func<string, IKustoManagementGateway> kustoManagementGatewayFactory,
            int maxDbs)
        {
            _dbPrefix = dbPrefix;
            _kustoManagementGatewayFactory = kustoManagementGatewayFactory;
            _maxDbs = maxDbs;
            _availableDbNames = Enumerable.Range(1, _maxDbs)
                .Select(i => DbNumberToDbName(i))
                .ToList();
        }
        #endregion

        public async Task<IImmutableList<string>> GetDbsAsync(int dbCount)
        {
            while (true)
            {
                var newDbEvent = _newDbEvent;

                lock (_queueLock)
                {
                    if (_availableDbNames.Count >= dbCount)
                    {
                        var dbNames = _availableDbNames.Take(dbCount).ToImmutableArray();

                        _availableDbNames.RemoveRange(0, dbCount);

                        return dbNames;
                    }
                }
                //  Not enough db available:  let's wait for some!
                await newDbEvent.Task;
                Interlocked.CompareExchange(
                    ref _newDbEvent,
                    new TaskCompletionSource(),
                    newDbEvent);
            }
        }

        public async Task CleanDbAsync(string dbName)
        {
            var kustoGateway = _kustoManagementGatewayFactory(dbName);
            var currentCommands = await kustoGateway.ReverseEngineerDatabaseAsync();
            var currentModel = DatabaseModel.FromCommands(currentCommands);
            var cleanCommands = currentModel.ComputeDelta(
                DatabaseModel.FromCommands(
                    ImmutableArray<CommandBase>.Empty));

            await kustoGateway.ExecuteCommandsAsync(cleanCommands);
        }

        public void ReleaseDbs(IEnumerable<string> dbNames)
        {
            lock (_queueLock)
            {
                _availableDbNames.AddRange(dbNames);
            }
            //  Pop event for waiting threads
            _newDbEvent.SetResult();
        }

        void IDisposable.Dispose()
        {
        }

        private string DbNumberToDbName(int c)
        {
            return $"{_dbPrefix}{c:D8}";
        }
    }
}