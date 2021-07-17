using DeltaKustoIntegration;
using DeltaKustoIntegration.Kusto;
using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib;
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
        private const int AHEAD_PROVISIONING_COUNT = 10;

        private readonly Lazy<string> _dbPrefix;
        private readonly Lazy<AzureManagementGateway> _azureManagementGateway;
        private readonly Lazy<Task> _initializedAsync;
        private readonly ConcurrentQueue<string> _createdQueue = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _deleteQueue = new ConcurrentQueue<string>();
        private readonly ManualResetEventSlim _newDbRequiredEvent = new ManualResetEventSlim(false);
        private Task? _backgroundTask = null;
        private volatile Task _newDbAvailableTask = Task.CompletedTask;
        private bool _isDisposing = false;

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
                    await CleanDbAsync();
                    //  Start provisioning
                    _newDbRequiredEvent.Set();
                },
                true);
            //  Forces the entire method to run on a different thread so not to lock this one
            //  (and create a deadlock)
            Task.Run(() =>
            {
                _backgroundTask = BackgroundAsync();
            });
        }

        public async Task<string> InitializeDbAsync()
        {
            await _initializedAsync.Value;

            if (_createdQueue.Count() < AHEAD_PROVISIONING_COUNT / 2)
            {   //  Preventive provisioning
                _newDbRequiredEvent.Set();
            }
            while (true)
            {   //  Is a db number been provisioned yet?
                string? dbName;

                if (_createdQueue.TryDequeue(out dbName))
                {
                    return dbName!;
                }
                else
                {
                    //  Signal background thread we need to create new dbs
                    _newDbRequiredEvent.Set();
                    await _newDbAvailableTask;
                }
            }
        }

        public void EnqueueDeleteDb(string dbName)
        {
            if (!dbName.StartsWith(_dbPrefix.Value))
            {
                throw new ArgumentException("Wrong prefix", nameof(dbName));
            }

            _deleteQueue.Enqueue(dbName);
        }

        void IDisposable.Dispose()
        {
            _isDisposing = true;
            if (_backgroundTask != null)
            {
                _backgroundTask.Wait();
            }
            CleanDbAsync().Wait();
        }

        private async Task CleanDbAsync()
        {
            var names = await _azureManagementGateway.Value.GetDatabaseNamesAsync();
            var deleteTasks = names
                .Where(n => n.StartsWith(_dbPrefix.Value))
                .Select(n => _azureManagementGateway.Value.DeleteDatabaseAsync(n));

            await Task.WhenAll(deleteTasks);
        }

        private async Task BackgroundAsync()
        {
            var taskSource = new TaskCompletionSource();
            var provisionedDbCount = 0;

            _newDbAvailableTask = taskSource.Task;

            while (!_isDisposing)
            {
                //  Wait first so that the first time we execute, the environment variables are loaded
                _newDbRequiredEvent.Wait();

                var provisioningCount = AHEAD_PROVISIONING_COUNT - _createdQueue.Count();
                var dbNames = Enumerable.Range(provisionedDbCount + 1, provisioningCount)
                    .Select(c => DbNumberToDbName(c))
                    .ToImmutableHashSet();
                var provisioningTasks = dbNames
                    .Select(dbName => _azureManagementGateway.Value.CreateDatabaseAsync(dbName));

                await Task.WhenAll(provisioningTasks);

                //  Delete after creating, letting more time for the creation to propagate
                //  and avoiding "database not found" errors
                var deleteTasks = EmptyCurrentDeleteQueue()
                    .Select(dbName => _azureManagementGateway.Value.DeleteDatabaseAsync(dbName));

                await Task.WhenAll(deleteTasks);

                //  Enqueue the new db names
                foreach (var dbName in dbNames)
                {
                    _createdQueue.Enqueue(dbName);
                }
                provisionedDbCount += dbNames.Count;
                //  Signal waiting consumer that new dbs are available
                taskSource.SetResult();
                //  Reset a new waiting task
                taskSource = new TaskCompletionSource();
                _newDbAvailableTask = taskSource.Task;
                //  Prepare to wait
                _newDbRequiredEvent.Reset();
            }
        }

        private string DbNumberToDbName(int c)
        {
            return $"{_dbPrefix.Value}{c.ToString("D8")}";
        }

        private IEnumerable<string> EmptyCurrentDeleteQueue()
        {
            var list = new List<string>();
            string? item;

            while (_deleteQueue.TryDequeue(out item))
            {
                if (item != null)
                {
                    list.Add(item);
                }
            }

            return list.ToImmutableArray();
        }
    }
}