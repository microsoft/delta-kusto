using DeltaKustoIntegration;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.KustoModel;
using DeltaKustoLib.SchemaObjects;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace delta_kusto
{
    internal class DeltaOrchestration
    {
        private readonly IFileGateway _fileGateway;
        private readonly IKustoManagementGatewayFactory _kustoManagementGatewayFactory;

        public DeltaOrchestration(
            IFileGateway fileGateway,
            IKustoManagementGatewayFactory kustoManagementGatewayFactory)
        {
            _fileGateway = fileGateway;
            _kustoManagementGatewayFactory = kustoManagementGatewayFactory;
        }

        public async Task ComputeDeltaAsync(string parameterFilePath)
        {
            var parameterText = await _fileGateway.GetFileContentAsync(parameterFilePath);
            var parameters = JsonSerializer.Deserialize<MainParameterization>(
                parameterText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            parameters.Validate();

            var tokenProvider = CreateTokenProvider(parameters.TokenProvider);

            foreach (var job in parameters.Jobs)
            {
                var currentDb = await RetrieveDatabase(job.Current, tokenProvider);
                var targetDb = await RetrieveDatabase(job.Target, tokenProvider);
                var deltaCommands = currentDb.ComputeDelta(targetDb);

                await ProcessDeltaCommandsAsync(deltaCommands, job.Action);
            }
        }

        private ITokenProvider? CreateTokenProvider(TokenProviderParameterization? tokenProvider)
        {
            throw new NotImplementedException();
        }

        private Task ProcessDeltaCommandsAsync(
            IEnumerable<CommandBase> deltaCommands,
            ActionParameterization? action)
        {
            throw new NotImplementedException();
        }

        private async Task<DatabaseModel> RetrieveDatabase(
            SourceParameterization? source,
            ITokenProvider? tokenProvider)
        {
            if (source == null)
            {
                return DatabaseModel.FromDatabaseSchema(new DatabaseSchema { Name = "empty-db" });
            }
            else
            {
                if (source.Cluster != null)
                {
                    if (tokenProvider == null)
                    {
                        throw new InvalidOperationException($"{tokenProvider} can't be null at this point");
                    }

                    var kustoManagementGateway = _kustoManagementGatewayFactory.CreateGateway(
                        source.Cluster.ClusterUri!,
                        source.Cluster.Database!,
                        tokenProvider);
                    var databaseSchema = await kustoManagementGateway.GetDatabaseSchemaAsync();

                    throw new NotImplementedException();
                }
                else if (source.Scripts != null)
                {
                    var scriptTasks = source
                        .Scripts
                        .Select(s => LoadScriptsAsync(s));

                    await Task.WhenAll(scriptTasks);

                    var commands = scriptTasks
                        .SelectMany(t => t.Result)
                        .SelectMany(s => CommandBase.FromScript(s))
                        .ToImmutableArray();
                    var database = DatabaseModel.FromCommands(commands);

                    return database;
                }
                else
                {
                    throw new InvalidOperationException("We should never get here");
                }
            }
        }

        private async Task<IEnumerable<string>> LoadScriptsAsync(SourceFileParametrization fileParametrization)
        {
            if (fileParametrization.FilePath != null)
            {
                var script = await _fileGateway.GetFileContentAsync(fileParametrization.FilePath);

                return new[] { script };
            }
            else if (fileParametrization.FolderPath != null)
            {
                var scripts = _fileGateway.GetFolderContentsAsync(
                    fileParametrization.FolderPath,
                    fileParametrization.Extensions);
                var contents = (await scripts.ToEnumerableAsync())
                    .Select(t => t.content);

                return contents;
            }
            else
            {
                throw new InvalidOperationException("We should never get here");
            }
        }
    }
}