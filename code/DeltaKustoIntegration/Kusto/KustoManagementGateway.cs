using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using Kusto.Data.Common;
using Kusto.Data.Exceptions;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Kusto
{
    /// <summary>
    /// Basically wraps REST APIs <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/api/rest/"/>.
    /// </summary>
    internal class KustoManagementGateway : IKustoManagementGateway
    {
        private static readonly ResiliencePipeline _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<KustoException>(ex => !ex.IsPermanent),
                MaxRetryAttempts = 5,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1)
            })
            .AddRetry(new RetryStrategyOptions
            {   //  Unauthorized
                ShouldHandle = new PredicateBuilder().Handle<KustoException>(ex => ex.FailureCode == 401),
                MaxRetryAttempts = 2,
                BackoffType = DelayBackoffType.Linear,
                Delay = TimeSpan.FromSeconds(2)
            })
            .Build();

        private readonly Uri _clusterUri;
        private readonly string _database;
        private readonly ICslAdminProvider _commandProvider;
        private readonly ITracer _tracer;
        private readonly ClientRequestProperties? _requestProperties;

        public KustoManagementGateway(
            Uri clusterUri,
            string database,
            ICslAdminProvider commandProvider,
            ITracer tracer,
            ClientRequestProperties? requestProperties)
        {
            _clusterUri = clusterUri;
            _database = database;
            _commandProvider = commandProvider;
            _tracer = tracer;
            _requestProperties = requestProperties;
        }

        async Task<IImmutableList<CommandBase>> IKustoManagementGateway.ReverseEngineerDatabaseAsync(
            CancellationToken ct)
        {
            var tracerTimer = new TracerTimer(_tracer);

            _tracer.WriteLine(true, "Fetch schema commands start");

            var schemaOutputTask = ExecuteCommandAsync(".show database schema as csl script", ct);
            var schemaOutput = await schemaOutputTask;

            _tracer.WriteLine(true, "Fetch schema commands end");
            tracerTimer.WriteTime(true, "Fetch schema commands time");

            var schemaCommandText = Select(
                schemaOutput,
                r => (string)r["DatabaseSchemaScript"]);
            var schemaCommands = CommandBase.FromScript(string.Join("\n\n", schemaCommandText), true);
            var allCommands = schemaCommands
                .ToImmutableArray();

            return allCommands;
        }

        async Task IKustoManagementGateway.ExecuteCommandsAsync(
            IEnumerable<CommandBase> commands,
            CancellationToken ct)
        {
            if (commands.Any())
            {
                _tracer.WriteLine(true, ".execute database script commands start");

                var tracerTimer = new TracerTimer(_tracer);
                var scriptingContext = new ScriptingContext
                {
                    CurrentDatabaseName = new EntityName(_database)
                };
                var commandScripts = commands.Select(c => c.ToScript(scriptingContext));
                var fullScript = ".execute database script with (ThrowOnErrors=true) <|"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine + Environment.NewLine, commandScripts);
                var output = await ExecuteCommandAsync(fullScript, ct);

                _tracer.WriteLine(true, ".execute database script commands end");
                tracerTimer.WriteTime(true, ".execute database script commands time");

                var content = Select(
                    output,
                    r => new
                    {
                        Result = (string)r["Result"],
                        Reason = (string)r["Reason"],
                        CommandText = (string)r["CommandText"],
                        OperationId = (Guid)r["OperationId"]
                    });
                var failedItems = content.Where(t => t.Result != "Completed");

                if (failedItems.Any())
                {
                    var failedItem = failedItems.First();
                    var failedItemCommand = PackageString(failedItem.CommandText);
                    var allCommands = string.Join(
                        ", ",
                        content.Select(i => $"({i.Result}:  '{PackageString(i.CommandText)}')"));

                    throw new InvalidOperationException(
                        $"Command failed to execute with reason '{failedItem.Reason}'.  "
                        + $"Operation ID:  {failedItem.OperationId}.  "
                        + $"Cluster URI:  {_clusterUri}.  "
                        + $"Database:  {_database}.  "
                        + $"Command:  '{failedItemCommand}'.  "
                        + $"All commands:  {allCommands}");
                }
            }
        }

        private static string PackageString(string text)
        {
            return text.Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static IEnumerable<T> Select<T>(IDataReader reader, Func<IDataReader, T> projection)
        {
            while (reader.Read())
            {
                yield return projection(reader);
            }
        }

        private async Task<IDataReader> ExecuteCommandAsync(
            string commandScript,
            CancellationToken ct)
        {
            try
            {
                return await _resiliencePipeline.ExecuteAsync(async (ct) =>
                {
                    var reader = await _commandProvider.ExecuteControlCommandAsync(
                        _database,
                        commandScript,
                        _requestProperties);

                    return reader;
                }, ct);
            }
            catch (Exception ex)
            {
                throw new DeltaException(
                    $"Issue running Kusto script '{commandScript}' on cluster '{_clusterUri}' / "
                    + $"database '{_database}'",
                    ex);
            }
        }
    }
}