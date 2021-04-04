using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.SchemaObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        #region Inner Types
        private class ApiOutput
        {
            public TableOutput[]? Tables { get; set; }

            public void Validate(string json)
            {
                if (Tables == null)
                {
                    throw new InvalidOperationException(
                        $"JSON payload doesn't contain a list of table "
                        + $"({nameof(Tables)}):  '{json}'");
                }

                foreach (var table in Tables)
                {
                    table.Validate(json);
                }
            }

            public T GetSingleElement<T>()
            {
                return Tables![0].GetSingleElement<T>();
            }

            public static ApiOutput FromJson(string json)
            {
                var output = JsonSerializer.Deserialize<ApiOutput>(json);

                if (output == null)
                {
                    throw new InvalidOperationException(
                        $"JSON payload doesn't look like an API output:  '{json}'");
                }

                output.Validate(json);

                return output;
            }
        }

        private class TableOutput
        {
            public string? TableName { get; set; }

            public ColumnOutput[]? Columns { get; set; }

            public JsonElement[][]? Rows { get; set; }

            public void Validate(string json)
            {
                if (TableName == null)
                {
                    throw new InvalidOperationException(
                        $"JSON payload has a table without name "
                        + $"({nameof(TableName)}):  '{json}'");
                }
                if (Columns == null)
                {
                    throw new InvalidOperationException(
                        $"Table '{TableName}' in JSON payload has no columns "
                        + $"({nameof(Columns)}):  '{json}'");
                }
                if (Rows == null)
                {
                    throw new InvalidOperationException(
                        $"Table '{TableName}' in JSON payload has no rows "
                        + $"({nameof(Rows)}):  '{json}'");
                }

                foreach (var column in Columns)
                {
                    column.Validate(json, TableName);
                }
                foreach (var row in Rows)
                {
                    if (row.Length != Columns.Length)
                    {
                        throw new InvalidOperationException(
                            $"Table '{TableName}' in JSON payload has a row with number of items "
                            + $"different than columns descriptions:  '{json}'");
                    }
                }
            }

            public T GetSingleElement<T>()
            {
                if (Rows!.Length == 0 || Rows![0].Length == 0)
                {
                    throw new InvalidOperationException(
                        $"Table '{TableName}' doesn't have any element");
                }

                return (T)GetObject(Rows![0][0], typeof(T));
            }

            public IEnumerable<T1> ProjectRows<T1>(string columnName)
            {
                var rows = ProjectRows(new[] { (columnName, typeof(T1)) });

                foreach (var row in rows)
                {
                    var item = (T1)row[0];

                    yield return item;
                }
            }

            public IEnumerable<(T1, T2)> ProjectRows<T1, T2>(
                string columnName1,
                string columnName2)
            {
                var rows = ProjectRows(new[]
                {
                    (columnName1, typeof(T1)),
                    (columnName2, typeof(T2))
                });

                foreach (var row in rows)
                {
                    var item1 = (T1)row[0];
                    var item2 = (T2)row[1];

                    yield return (item1, item2);
                }
            }

            public IEnumerable<(T1, T2, T3)> ProjectRows<T1, T2, T3>(
                string columnName1,
                string columnName2,
                string columnName3)
            {
                var rows = ProjectRows(new[]
                {
                    (columnName1, typeof(T1)),
                    (columnName2, typeof(T2)),
                    (columnName3, typeof(T3))
                });

                foreach (var row in rows)
                {
                    var item1 = (T1)row[0];
                    var item2 = (T2)row[1];
                    var item3 = (T3)row[2];

                    yield return (item1, item2, item3);
                }
            }

            public IEnumerable<(T1, T2, T3, T4)> ProjectRows<T1, T2, T3, T4>(
                string columnName1,
                string columnName2,
                string columnName3,
                string columnName4)
            {
                var rows = ProjectRows(new[]
                {
                    (columnName1, typeof(T1)),
                    (columnName2, typeof(T2)),
                    (columnName3, typeof(T3)),
                    (columnName4, typeof(T4))
                });

                foreach (var row in rows)
                {
                    var item1 = (T1)row[0];
                    var item2 = (T2)row[1];
                    var item3 = (T3)row[2];
                    var item4 = (T4)row[3];

                    yield return (item1, item2, item3, item4);
                }
            }

            public IEnumerable<object[]> ProjectRows(params (string name, Type type)[] columnNameTypes)
            {
                var indexes = MapColumns(columnNameTypes);

                foreach (var row in Rows!)
                {
                    var item = new object[indexes.Length];

                    for (int i = 0; i != indexes.Length; ++i)
                    {
                        item[i] = GetObject(row[indexes[i]], columnNameTypes[i].type);
                    }

                    yield return item;
                }
            }

            private object GetObject(JsonElement element, Type type)
            {
                if (type == typeof(string))
                {
                    return element.GetString()!;
                }
                else if (type == typeof(int))
                {
                    return element.GetInt32();
                }
                else if (type == typeof(Guid))
                {
                    return element.GetGuid();
                }
                else
                {
                    throw new NotSupportedException(
                        $"Type '{type}' isn't supported for table columns");
                }
            }

            private int[] MapColumns((string name, Type type)[] columnNameTypes)
            {
                var indexes = new int[columnNameTypes.Length];
                var columnIndex = Columns!.Zip(
                    Enumerable.Range(0, Columns!.Length),
                    (c, i) => new { Name = c.ColumnName!, TypeName = c.ColumnType!, Index = i })
                    .ToDictionary(c => c.Name);

                for (int i = 0; i != columnNameTypes.Length; ++i)
                {
                    var columnName = columnNameTypes[i].name;
                    var columnType = columnNameTypes[i].type;

                    if (!columnIndex.ContainsKey(columnName))
                    {
                        throw new InvalidOperationException(
                            $"Can't find column '{columnName}' "
                            + $"in table '{TableName}' of Kusto result");
                    }

                    var column = columnIndex[columnName];

                    if (string.Compare(column.TypeName, columnType.Name, true) != 0)
                    {
                        throw new InvalidOperationException(
                            $"Type mismatch for column '{columnName}' "
                            + $"in table '{TableName}' of Kusto result:  "
                            + $"expected '{columnType.Name}' but is {column.TypeName}");
                    }

                    indexes[i] = column.Index;
                }

                return indexes;
            }
        }

        private class ColumnOutput
        {
            public string? ColumnName { get; set; }

            public string? DataType { get; set; }

            public string? ColumnType { get; set; }

            public void Validate(string json, string tableName)
            {
                if (ColumnName == null)
                {
                    throw new InvalidOperationException(
                        $"Table '{tableName}' in JSON payload has a column "
                        + $"without name ({nameof(ColumnName)}):  '{json}'");
                }
                if (DataType == null)
                {
                    throw new InvalidOperationException(
                        $"Table '{tableName}' in JSON payload has column '{ColumnName}' "
                        + $"without data type ({nameof(DataType)}):  '{json}'");
                }
                if (ColumnType == null)
                {
                    throw new InvalidOperationException(
                        $"Table '{tableName}' in JSON payload has column '{ColumnName}' "
                        + $"without column type ({nameof(ColumnType)}):  '{json}'");
                }
            }
        }
        #endregion

        private readonly ITracer _tracer;
        private readonly Uri _clusterUri;
        private readonly string _database;
        private readonly ITokenProvider _tokenProvider;

        public KustoManagementGateway(
            ITracer tracer,
            Uri clusterUri,
            string database,
            ITokenProvider tokenProvider)
        {
            _tracer = tracer;
            _clusterUri = clusterUri;
            _database = database;
            _tokenProvider = tokenProvider;
        }

        async Task<DatabaseSchema> IKustoManagementGateway.GetDatabaseSchemaAsync(
            CancellationToken ct)
        {
            _tracer.WriteLine(true, ".show db command start");

            var output = await ExecuteCommandAsync(".show database schema as json", ct);
            
            _tracer.WriteLine(true, ".show db command end");
            
            var schemaText = output.GetSingleElement<string>();
            var rootSchema = RootSchema.FromJson(schemaText);

            if (rootSchema.Databases.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Schema doesn't contain a database:  '{schemaText}'");
            }

            return rootSchema.Databases.First().Value;
        }

        async Task IKustoManagementGateway.ExecuteCommandsAsync(
            IEnumerable<CommandBase> commands,
            CancellationToken ct)
        {
            if (commands.Any())
            {
                _tracer.WriteLine(true, ".execute database script commands start");

                var commandScripts = commands.Select(c => c.ToScript());
                var fullScript = ".execute database script <|"
                    + Environment.NewLine
                    + string.Join(Environment.NewLine + Environment.NewLine, commandScripts);
                var output = await ExecuteCommandAsync(fullScript, ct);
                
                _tracer.WriteLine(true, ".execute database script commands end");
                
                var content = output.Tables![0].ProjectRows<string, string, string, Guid>(
                    "Result",
                    "Reason",
                    "CommandText",
                    "OperationId")
                    .Select(t => (result: t.Item1, reason: t.Item2, commandText: t.Item3, operationId: t.Item4));
                var failedItems = content.Where(t => t.result != "Completed");

                if (failedItems.Any())
                {
                    var failedItem = failedItems.First();
                    var failedItemCommand = PackageString(failedItem.commandText);
                    var allCommands = string.Join(
                        ", ",
                        content.Select(i => $"({i.result}:  '{PackageString(i.commandText)}')"));

                    throw new InvalidOperationException(
                        $"Command failed to execute with reason '{failedItem.reason}'.  "
                        + $"Operation ID:  {failedItem.operationId}.  "
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

        private async Task<ApiOutput> ExecuteCommandAsync(
            string commandScript,
            CancellationToken ct)
        {
            try
            {
                var token = await _tokenProvider.GetTokenAsync(_clusterUri, ct);

                //  Implementation of https://docs.microsoft.com/en-us/azure/data-explorer/kusto/api/rest/request#examples
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var managementUrl = $"{_clusterUri}/v1/rest/mgmt";
                    var body = new
                    {
                        db = _database,
                        csl = commandScript
                    };
                    var bodyText = JsonSerializer.Serialize(body);
                    var response = await client.PostAsync(
                        managementUrl,
                        new StringContent(bodyText, null, "application/json"),
                        ct);

                    _tracer.WriteLine(true, "KustoManagementGateway.ExecuteCommandAsync retrieve payload");

                    var responseText =
                        await response.Content.ReadAsStringAsync(ct);

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new InvalidOperationException(
                            $"'{body.csl}' command failed for cluster URI '{_clusterUri}' "
                            + $"with status code '{response.StatusCode}' "
                            + $"and payload '{responseText}'");
                    }

                    var output = ApiOutput.FromJson(responseText);

                    return output;
                }
            }
            catch (Exception ex)
            {
                throw new DeltaException(
                    $"Issue running Kusto script on cluster '{_clusterUri}' / "
                    + $"database '{_database}'",
                    ex);
            }
        }
    }
}