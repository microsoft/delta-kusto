using DeltaKustoIntegration;
using DeltaKustoIntegration.Action;
using DeltaKustoIntegration.Database;
using DeltaKustoIntegration.Kusto;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib;
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
        private readonly bool _verbose;
        private readonly IFileGateway _fileGateway;
        private readonly IKustoManagementGatewayFactory _kustoManagementGatewayFactory;
        private readonly ITokenProviderFactory _tokenProviderFactory;

        public DeltaOrchestration(
            bool verbose = false,
            IFileGateway? fileGateway = null,
            IKustoManagementGatewayFactory? kustoManagementGatewayFactory = null,
            ITokenProviderFactory? tokenProviderFactory = null)
        {
            _verbose = verbose;
            _fileGateway = fileGateway ?? new FileGateway();
            _kustoManagementGatewayFactory = kustoManagementGatewayFactory
                ?? new KustoManagementGatewayFactory();
            _tokenProviderFactory = tokenProviderFactory ?? new TokenProviderFactory();
        }



        public async Task<bool> ComputeDeltaAsync(
            string parameterFilePath,
            string jsonOverrides,
            CancellationToken ct)
        {
            Console.WriteLine($"Loading parameters at '{parameterFilePath}'");

            var parameters =
                await LoadParameterizationAsync(parameterFilePath, jsonOverrides, ct);

            try
            {
                var tokenProvider = _tokenProviderFactory.CreateProvider(parameters.TokenProvider);
                var orderedJobs = parameters.Jobs.OrderBy(p => p.Value.Priority);
                var success = true;

                Console.WriteLine($"{orderedJobs.Count()} jobs");

                foreach (var jobPair in orderedJobs)
                {
                    var (jobName, job) = jobPair;
                    var jobSuccess = await ProcessJobAsync(
                        parameters,
                        tokenProvider,
                        jobName,
                        job,
                        ct);

                    success = success && jobSuccess;
                }

                return success;
            }
            catch (Exception ex)
            {
                if (parameters.SendErrorOptIn)
                {
                    await ApiClient.RegisterExceptionAsync(ex, ct);
                }
                throw;
            }
        }

        private async Task<bool> ProcessJobAsync(
            MainParameterization parameters,
            ITokenProvider? tokenProvider,
            string jobName,
            JobParameterization job,
            CancellationToken ct)
        {
            Console.WriteLine($"Job {jobName}");
            try
            {
                var currentDbProvider = CreateDatabaseProvider(job.Current, tokenProvider);
                var targetDbProvider = CreateDatabaseProvider(job.Target, tokenProvider);
                var actionProviders = CreateActionProvider(
                    job.Action!,
                    tokenProvider,
                    job.Current?.Adx);
                var currentDb = await currentDbProvider.RetrieveDatabaseAsync(ct);
                var targetDb = await targetDbProvider.RetrieveDatabaseAsync(ct);
                var deltaCommands =
                    new ActionCommandCollection(currentDb.ComputeDelta(targetDb));
                var jobSuccess = ReportOnDeltaCommands(parameters, deltaCommands);

                foreach (var actionProvider in actionProviders)
                {
                    await actionProvider.ProcessDeltaCommandsAsync(
                        parameters.FailIfDrops,
                        deltaCommands,
                        ct);
                }

                return jobSuccess;
            }
            catch (DeltaException ex)
            {
                throw new DeltaException($"Issue in running job '{jobName}'", ex);
            }
        }

        private static bool ReportOnDeltaCommands(
            MainParameterization parameters,
            ActionCommandCollection deltaCommands)
        {
            var success = true;

            Console.WriteLine($"{deltaCommands.Count()} commands in delta");
            if (deltaCommands.AllDropCommands.Any())
            {
                Console.WriteLine("Delta contains drop commands:");
                foreach (var command in deltaCommands.AllDropCommands)
                {
                    Console.WriteLine("  " + command.ToScript());
                }
                Console.WriteLine();
                if (parameters.FailIfDrops)
                {
                    Console.Error.WriteLine("Drop commands forces failure");
                    success = false;
                }
            }
            Console.WriteLine("Processing delta commands");

            return success;
        }

        internal async Task<MainParameterization> LoadParameterizationAsync(
            string parameterFilePath,
            string jsonOverrides,
            CancellationToken ct)
        {
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

                ParameterOverrideHelper.InplaceOverride(parameters, jsonOverrides);

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

            if (action.FilePath != null)
            {
                builder.Add(new OneFileActionProvider(_fileGateway, action.FilePath));
            }
            if (action.FolderPath != null)
            {
                builder.Add(new MultiFilesActionProvider(_fileGateway, action.FolderPath));
            }
            if (action.PushToConsole)
            {
                throw new NotImplementedException();
            }
            if (action.PushToCurrentCluster)
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
            if (builder.Count() == 0)
            {
                throw new InvalidOperationException("We should never get here");
            }

            return builder.ToImmutable();
        }

        private IDatabaseProvider CreateDatabaseProvider(
            SourceParameterization? source,
            ITokenProvider? tokenProvider)
        {
            if (source == null)
            {
                return new EmptyDatabaseProvider();
            }
            else
            {
                if (source.Adx != null)
                {
                    if (tokenProvider == null)
                    {
                        throw new InvalidOperationException($"{tokenProvider} can't be null at this point");
                    }

                    var kustoManagementGateway = _kustoManagementGatewayFactory.CreateGateway(
                        new Uri(source.Adx.ClusterUri!),
                        source.Adx.Database!,
                        tokenProvider);

                    return new KustoDatabaseProvider(kustoManagementGateway);
                }
                else if (source.Scripts != null)
                {
                    return new ScriptDatabaseProvider(_fileGateway, source.Scripts);
                }
                else
                {
                    throw new InvalidOperationException("We should never get here");
                }
            }
        }
    }
}