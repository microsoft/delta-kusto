using DeltaKustoIntegration.TokenProvider;
using DeltaKustoLib;
using DeltaKustoLib.SchemaObjects;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeltaKustoIntegration.Kusto
{
    /// <summary>
    /// Basically wraps REST APIs <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/api/rest/"/>.
    /// </summary>
    internal class KustoManagementGateway : IKustoManagementGateway
    {
        #region Inner Types
        private class ApiOutput<T>
        {
            public TableOutput<T>[]? Tables { get; set; }

            public void Validate(string json)
            {
                if (Tables == null)
                {
                    throw new DeltaException(
                        $"JSON payload doesn't contain a list of table "
                        + $"({nameof(Tables)}):  '{json}'");
                }

                foreach (var table in Tables)
                {
                    table.Validate(json);
                }
            }

            public T GetSingleElement()
            {
                return Tables![0].Rows![0][0];
            }

            public static ApiOutput<T> FromJson(string json)
            {
                var output = JsonSerializer.Deserialize<ApiOutput<T>>(json);

                if (output == null)
                {
                    throw new DeltaException(
                        $"JSON payload doesn't look like an API output:  '{json}'");
                }

                output.Validate(json);

                return output;
            }
        }

        private class TableOutput<T>
        {
            public string? TableName { get; set; }

            public ColumnOutput[]? Columns { get; set; }

            public T[][]? Rows { get; set; }

            public void Validate(string json)
            {
                if (TableName == null)
                {
                    throw new DeltaException(
                        $"JSON payload has a table without name "
                        + $"({nameof(TableName)}):  '{json}'");
                }
                if (Columns == null)
                {
                    throw new DeltaException(
                        $"Table '{TableName}' in JSON payload has no columns "
                        + $"({nameof(Columns)}):  '{json}'");
                }
                if (Rows == null)
                {
                    throw new DeltaException(
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
                        throw new DeltaException(
                            $"Table '{TableName}' in JSON payload has a row with number of items "
                            + $"different than columns descriptions:  '{json}'");
                    }
                }
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
                    throw new DeltaException(
                        $"Table '{tableName}' in JSON payload has a column "
                        + $"without name ({nameof(ColumnName)}):  '{json}'");
                }
                if (DataType == null)
                {
                    throw new DeltaException(
                        $"Table '{tableName}' in JSON payload has column '{ColumnName}' "
                        + $"without data type ({nameof(DataType)}):  '{json}'");
                }
                if (ColumnType == null)
                {
                    throw new DeltaException(
                        $"Table '{tableName}' in JSON payload has column '{ColumnName}' "
                        + $"without column type ({nameof(ColumnType)}):  '{json}'");
                }
            }
        }
        #endregion

        private readonly Uri _clusterUri;
        private readonly string _database;
        private readonly ITokenProvider _tokenProvider;

        public KustoManagementGateway(Uri clusterUri, string database, ITokenProvider tokenProvider)
        {
            _clusterUri = clusterUri;
            _database = database;
            _tokenProvider = tokenProvider;
        }

        async Task<DatabaseSchema> IKustoManagementGateway.GetDatabaseSchemaAsync()
        {
            var token = await _tokenProvider.GetTokenAsync(_clusterUri);

            //  Implementation of https://docs.microsoft.com/en-us/azure/data-explorer/kusto/api/rest/request#examples
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var managementUrl = $"{_clusterUri}/v1/rest/mgmt";
                var body = new
                {
                    db = _database,
                    csl = ".show database schema as json"
                };
                var bodyText = JsonSerializer.Serialize(body);
                var response = await client.PostAsync(
                    managementUrl,
                    new StringContent(bodyText, null, "application/json"));
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new DeltaException(
                        $"'{body.csl}' command failed for cluster URI '{_clusterUri}' "
                        + $"with status code '{response.StatusCode}' "
                        + $"and payload '{responseText}'");
                }

                var output = ApiOutput<string>.FromJson(responseText);
                var schemaText = output.GetSingleElement();
                var rootSchema = RootSchema.FromJson(schemaText);

                if (rootSchema.Databases.Count != 1)
                {
                    throw new DeltaException(
                        $"Schema doesn't contain a database:  '{schemaText}'");
                }

                return rootSchema.Databases.First().Value;
            }
        }
    }
}