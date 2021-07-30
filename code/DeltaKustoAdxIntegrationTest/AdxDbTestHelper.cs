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
        private const int AHEAD_PROVISIONING_COUNT = 10;

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
        var d = Environment.GetEnvironmentVariables();

        foreach (var key in d.Keys.Cast<string>().Where(k => k.StartsWith("delta")))
        {
            var value = d[key]!.ToString();
            var withDots = string.Join('.', value!.Select(c => c.ToString()));

            Console.WriteLine($"{key}:  ({withDots})");
        }
        _dbPrefix = dbPrefix;
        _kustoManagementGatewayFactory = kustoManagementGatewayFactory;
        _azureManagementGateway = azureManagementGateway;
        _initializedAsync = DeleteAllDbsAsync();
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
        {   //  Prepare DBs in advance
            while (_preparingDbs.Count < AHEAD_PROVISIONING_COUNT)
            {
                _preparingDbs.Enqueue(CreateDbAsync());
            }
            var dbName = await CreateDbAsync();

            return dbName;
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
        DeleteAllDbsAsync().Wait();
    }

    private async Task<string> CreateDbAsync()
    {
        var dbName = DbNumberToDbName(Interlocked.Increment(ref _dbCount));

        await _azureManagementGateway.CreateDatabaseAsync(dbName);

        var kustoGateway = _kustoManagementGatewayFactory(dbName);

        while (!(await kustoGateway.DoesDatabaseExistsAsync()))
        {
            await Task.Delay(TimeSpan.FromSeconds(.2));
        }

        //  Event newly created databases might contain default policies we want to get rid of
        await CleanDbAsync(dbName);

        return dbName;
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

    private async Task DeleteAllDbsAsync()
    {
        var names = await _azureManagementGateway.GetDatabaseNamesAsync();
        var deleteTasks = names
            .Where(n => n.StartsWith(_dbPrefix))
            .Select(n => _azureManagementGateway.DeleteDatabaseAsync(n));

        await Task.WhenAll(deleteTasks);
    }

    private string DbNumberToDbName(int c)
    {
        return $"{_dbPrefix}{c.ToString("D8")}";
    }
}
}