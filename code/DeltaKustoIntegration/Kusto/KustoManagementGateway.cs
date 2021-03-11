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

                var schema = DatabaseSchema.FromJson(responseText);

                //if (rootSchema.Databases.Count != 1)
                //{
                //    throw new DeltaException(
                //        $"Schema doesn't contain a database:  '{responseText}'");
                //}

                return schema;
            }
        }
    }
}