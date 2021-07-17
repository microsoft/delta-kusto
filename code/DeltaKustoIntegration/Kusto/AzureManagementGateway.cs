using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    /// Basically wraps REST APIs <see cref="https://docs.microsoft.com/en-us/rest/api/azurerekusto/databases"/>.
    /// </summary>
    internal class AzureManagementGateway
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

            public TableOutput GetFirstTable()
            {
                if (Tables == null || !Tables.Any())
                {
                    throw new DeltaException("No table were returned from API call to Kusto");
                }

                return Tables.First();
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

        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(10);

        private readonly string _clusterId;
        private readonly ITokenProvider _tokenProvider;
        private readonly ITracer _tracer;
        private readonly SimpleHttpClientFactory _httpClientFactory;

        public AzureManagementGateway(
            string clusterId,
            ITokenProvider tokenProvider,
            ITracer tracer,
            SimpleHttpClientFactory httpClientFactory)
        {
            _clusterId = clusterId;
            _tokenProvider = tokenProvider;
            _tracer = tracer;
            _httpClientFactory = httpClientFactory;
        }

        public async Task CreateDatabaseAsync(string dbName, CancellationToken ct = default)
        {
            var tracerTimer = new TracerTimer(_tracer);

            _tracer.WriteLine(true, "Create Database start");
            _tracer.WriteLine(true, $"Database name:  '{dbName}'");

            using(var client = await CreateHttpClient(ct))
            {
                ct = CancellationTokenHelper.MergeCancellationToken(ct, TIMEOUT);

                var apiUrl = $"https://management.azure.com{_clusterId}/databases/{dbName}?api-version=2021-01-01";
                var response = await client.PutAsync(
                    apiUrl,
                    new StringContent("{\"kind\":\"ReadWrite\", \"location\":\"East US\"}", null, "application/json"),
                    ct);

                _tracer.WriteLine(true, "Database created");

                var responseText =
                    await response.Content.ReadAsStringAsync(ct);

                if (response.StatusCode != HttpStatusCode.Created)
                {
                    throw new InvalidOperationException(
                        $"Database '{dbName}' creation failed on cluster ID {_clusterId} "
                        + $"with status code '{response.StatusCode}' "
                        + $"and payload '{responseText}'");
                }
            }
        }

        private async Task<HttpClient> CreateHttpClient(CancellationToken ct)
        {
            try
            {
                ct = CancellationTokenHelper.MergeCancellationToken(ct, TIMEOUT);

                var token = await _tokenProvider.GetTokenAsync("https://management.azure.com", ct);
                var client = _httpClientFactory.CreateHttpClient();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                return client;
            }
            catch (Exception ex)
            {
                throw new DeltaException($"Issue preparing connection to Azure Management", ex);
            }
        }
    }
}