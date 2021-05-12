using DeltaKustoIntegration;
using DeltaKustoIntegration.Action;
using DeltaKustoIntegration.Database;
using DeltaKustoIntegration.Kusto;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace delta_kusto
{
    internal class DeltaOrchestration
    {
        private readonly ITracer _tracer;
        private readonly ApiClient _apiClient;
        private readonly IFileGateway _fileGateway;
        private readonly IKustoManagementGatewayFactory _kustoManagementGatewayFactory;
        private readonly ITokenProviderFactory _tokenProviderFactory;

        public DeltaOrchestration(
            ITracer tracer,
            ApiClient apiClient,
            IKustoManagementGatewayFactory kustoManagementGatewayFactory,
            ITokenProviderFactory tokenProviderFactory,
            IFileGateway? fileGateway = null)
        {
            _tracer = tracer;
            _apiClient = apiClient;
            _kustoManagementGatewayFactory = kustoManagementGatewayFactory;
            _tokenProviderFactory = tokenProviderFactory;
            _fileGateway = fileGateway ?? new FileGateway();
        }

        public async Task<bool> ComputeDeltaAsync(
            string parameterFilePath,
            IEnumerable<string> pathOverrides)
        {
            _tracer.WriteLine(false, "Activating Client...");

            var availableClientVersions = await _apiClient.ActivateAsync();

            if (availableClientVersions == null)
            {
                _tracer.WriteLine(false, "Activating Client (skipped)");
            }
            else
            {
                _tracer.WriteLine(false, "Client Activated");
                if (availableClientVersions.Any())
                {
                    _tracer.WriteLine(
                        false,
                        "Newer clients available:  "
                        + string.Join(", ", availableClientVersions));
                }
            }

            _tracer.WriteLine(false, $"Loading parameters at '{parameterFilePath}'");

            var parameters =
                await LoadParameterizationAsync(parameterFilePath, pathOverrides);
            var parameterFolderPath = Path.GetDirectoryName(parameterFilePath);

            try
            {
                var fileGateway = _fileGateway.ChangeFolder(parameterFolderPath!);
                var tokenProvider = _tokenProviderFactory.CreateProvider(parameters.TokenProvider);
                var orderedJobs = parameters.Jobs.OrderBy(p => p.Value.Priority);
                var success = true;

                _tracer.WriteLine(false, $"{orderedJobs.Count()} jobs");

                foreach (var jobPair in orderedJobs)
                {
                    var (jobName, job) = jobPair;
                    var jobSuccess = await ProcessJobAsync(
                        parameters,
                        fileGateway,
                        tokenProvider,
                        jobName,
                        job);

                    success = success && jobSuccess;
                }

                return success;
            }
            catch (Exception ex)
            {
                if (parameters.SendErrorOptIn)
                {
                    var operationID = await _apiClient.RegisterExceptionAsync(ex);

                    _tracer.WriteLine(
                        false,
                        $"Exception registered with Operation ID '{operationID}'");
                }
                throw;
            }
        }

        private async Task<bool> ProcessJobAsync(
            MainParameterization parameters,
            IFileGateway fileGateway,
            ITokenProvider? tokenProvider,
            string jobName,
            JobParameterization job)
        {
            _tracer.WriteLine(false, $"Job '{jobName}':");
            _tracer.WriteLine(false, "");
            try
            {
                _tracer.WriteLine(false, "Current DB Provider...  ");

                var currentDbProvider =
                    CreateDatabaseProvider(job.Current, tokenProvider, fileGateway);

                _tracer.WriteLine(false, "Target DB Provider...  ");

                var targetDbProvider =
                    CreateDatabaseProvider(job.Target, tokenProvider, fileGateway);
                var tokenSourceRetrieveDb = new CancellationTokenSource(TimeOuts.RETRIEVE_DB);
                var ctRetrieveDb = tokenSourceRetrieveDb.Token;

                var currentDbTask = RetrieveDatabaseAsync(currentDbProvider, "current", ctRetrieveDb);
                var targetDbTask = RetrieveDatabaseAsync(targetDbProvider, "target", ctRetrieveDb);

                await Task.WhenAll(currentDbTask, targetDbTask);

                var currentDb = await currentDbTask;
                var targetDb = await targetDbTask;

                _tracer.WriteLine(false, "Compute Delta...");

                var delta = currentDb.ComputeDelta(targetDb);
                var actions = new ActionCommandCollection(delta);
                var jobSuccess = ReportOnDeltaCommands(parameters, actions);
                var actionProviders = CreateActionProvider(
                    job.Action!,
                    tokenProvider,
                    job.Current?.Adx);
                var tokenSourceAction = new CancellationTokenSource(TimeOuts.ACTION);
                var ctAction = tokenSourceRetrieveDb.Token;

                _tracer.WriteLine(false, "Processing delta commands...");
                foreach (var actionProvider in actionProviders)
                {
                    await actionProvider.ProcessDeltaCommandsAsync(
                        parameters.FailIfDataLoss,
                        actions,
                        ctAction);
                }
                _tracer.WriteLine(false, "Delta processed / Job completed");
                _tracer.WriteLine(false, "");

                return jobSuccess;
            }
            catch (DeltaException ex)
            {
                throw new DeltaException($"Issue in running job '{jobName}'", ex);
            }
        }

        private async Task<DatabaseModel> RetrieveDatabaseAsync(
            IDatabaseProvider currentDbProvider,
            string db,
            CancellationToken ct)
        {
            _tracer.WriteLine(false, $"Retrieving {db}...");

            var model = await currentDbProvider.RetrieveDatabaseAsync(ct);

            _tracer.WriteLine(false, $"{db} retrieved");

            return model;
        }

        private bool ReportOnDeltaCommands(
            MainParameterization parameters,
            ActionCommandCollection deltaCommands)
        {
            var success = true;

            _tracer.WriteLine(false, $"{deltaCommands.Count()} commands in delta");
            if (deltaCommands.AllDataLossCommands.Any())
            {
                _tracer.WriteLine(false, "Delta contains drop commands:");
                foreach (var command in deltaCommands.AllDataLossCommands)
                {
                    _tracer.WriteLine(false, "  " + command.ToScript());
                }
                _tracer.WriteLine(false, "");
                if (parameters.FailIfDataLoss)
                {
                    _tracer.WriteErrorLine("Drop commands forces failure");
                    success = false;
                }
            }

            return success;
        }

        internal async Task<MainParameterization> LoadParameterizationAsync(
            string parameterFilePath,
            IEnumerable<string> pathOverrides)
        {
            var tokenSource = new CancellationTokenSource(TimeOuts.FILE);
            var ct = tokenSource.Token;

            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                var parameterText = await _fileGateway.GetFileContentAsync(parameterFilePath, ct);
                var parameters = deserializer.Deserialize<MainParameterization>(parameterText);

                if (parameters == null)
                {
                    throw new DeltaException($"File '{parameterFilePath}' doesn't contain valid parameters");
                }

                ParameterOverrideHelper.InplaceOverride(parameters, pathOverrides);

                parameters.Validate();

                return parameters;
            }
            catch (JsonException ex)
            {
                throw new DeltaException(
                    $"Issue reading the parameter file '{parameterFilePath}'",
                    ex);
            }
        }

        private IImmutableList<IActionProvider> CreateActionProvider(
            ActionParameterization action,
            ITokenProvider? tokenProvider,
            AdxSourceParameterization? database)
        {
            var builder = ImmutableArray<IActionProvider>.Empty.ToBuilder();

            builder.Add(new ConsoleActionProvider(_tracer, !action.PushToConsole));

            if (action.FilePath != null)
            {
                builder.Add(new OneFileActionProvider(
                    _fileGateway,
                    action.FilePath,
                    action.UsePluralForms));
            }
            if (action.FolderPath != null)
            {
                builder.Add(new MultiFilesActionProvider(
                    _fileGateway,
                    action.FolderPath,
                    action.UsePluralForms));
            }
            if (action.PushToCurrent)
            {
                if (tokenProvider == null)
                {
                    throw new InvalidOperationException(
                        $"{tokenProvider} can't be null at this point");
                }

                var kustoManagementGateway = _kustoManagementGatewayFactory.CreateGateway(
                    new Uri(database!.ClusterUri!),
                    database!.Database!,
                    tokenProvider);

                builder.Add(new KustoActionProvider(kustoManagementGateway));
            }

            return builder.ToImmutable();
        }

        private IDatabaseProvider CreateDatabaseProvider(
            SourceParameterization? source,
            ITokenProvider? tokenProvider,
            IFileGateway fileGateway)
        {
            if (source == null)
            {
                _tracer.WriteLine(true, "Empty database");

                return new EmptyDatabaseProvider();
            }
            else
            {
                if (source.Adx != null)
                {
                    _tracer.WriteLine(
                        true,
                        $"ADX Database:  cluster '{source.Adx.ClusterUri}', "
                        + $"database '{source.Adx.Database}'");

                    if (tokenProvider == null)
                    {
                        throw new InvalidOperationException($"{tokenProvider} can't be null at this point");
                    }

                    var kustoManagementGateway = _kustoManagementGatewayFactory.CreateGateway(
                        new Uri(source.Adx.ClusterUri!),
                        source.Adx.Database!,
                        tokenProvider);

                    return new KustoDatabaseProvider(_tracer, kustoManagementGateway);
                }
                else if (source.Scripts != null)
                {
                    _tracer.WriteLine(true, "Database scripts");

                    return new ScriptDatabaseProvider(_tracer, _fileGateway, source.Scripts);
                }
                else if (source.JsonFilePath != null)
                {
                    _tracer.WriteLine(true, "Json file");

                    return new JsonDatabaseProvider(_tracer, _fileGateway, source.JsonFilePath);
                }
                else
                {
                    throw new InvalidOperationException("We should never get here");
                }
            }
        }
    }
}