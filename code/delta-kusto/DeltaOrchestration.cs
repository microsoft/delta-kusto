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
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace delta_kusto
{
    internal class DeltaOrchestration
    {
        private readonly IFileGateway _fileGateway;
        private readonly IKustoManagementGatewayFactory _kustoManagementGatewayFactory;
        private readonly ITokenProviderFactory _tokenProviderFactory;

        public DeltaOrchestration(
            IFileGateway? fileGateway = null,
            IKustoManagementGatewayFactory? kustoManagementGatewayFactory = null,
            ITokenProviderFactory? tokenProviderFactory = null)
        {
            _fileGateway = fileGateway ?? new FileGateway();
            _kustoManagementGatewayFactory = kustoManagementGatewayFactory
                ?? new KustoManagementGatewayFactory();
            _tokenProviderFactory = tokenProviderFactory ?? new TokenProviderFactory();
        }

        public async Task ComputeDeltaAsync(string parameterFilePath, string jsonOverrides)
        {
            var parameters = await LoadParameterizationAsync(parameterFilePath, jsonOverrides);
            var tokenProvider = _tokenProviderFactory.CreateProvider(parameters.TokenProvider);
            var orderedJobs = parameters.Jobs.OrderBy(p => p.Value.Priority);

            foreach (var jobPair in orderedJobs)
            {
                var (jobName, job) = jobPair;

                try
                {
                    var currentDbProvider = CreateDatabaseProvider(job.Current, tokenProvider);
                    var targetDbProvider = CreateDatabaseProvider(job.Target, tokenProvider);
                    var actionProviders = CreateActionProvider(
                        job.Action!,
                        tokenProvider,
                        job.Current?.Database);
                    var currentDb = await currentDbProvider.RetrieveDatabaseAsync();
                    var targetDb = await targetDbProvider.RetrieveDatabaseAsync();
                    var deltaCommands = currentDb.ComputeDelta(targetDb);

                    foreach (var actionProvider in actionProviders)
                    {
                        await actionProvider.ProcessDeltaCommandsAsync(deltaCommands);
                    }
                }
                catch (DeltaException ex)
                {
                    throw new DeltaException($"Issue in running job '{jobName}'", ex);
                }
            }
        }

        internal async Task<MainParameterization> LoadParameterizationAsync(
            string parameterFilePath,
            string jsonOverrides)
        {
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                var parameterText = await _fileGateway.GetFileContentAsync(parameterFilePath);
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
            DatabaseSourceParameterization? database)
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
                if (source.Database != null)
                {
                    if (tokenProvider == null)
                    {
                        throw new InvalidOperationException($"{tokenProvider} can't be null at this point");
                    }

                    var kustoManagementGateway = _kustoManagementGatewayFactory.CreateGateway(
                        new Uri(source.Database.ClusterUri!),
                        source.Database.Database!,
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