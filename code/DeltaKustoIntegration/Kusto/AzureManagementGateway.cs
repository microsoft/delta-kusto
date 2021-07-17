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
        private record DatabaseListOutput(DatabaseListOutputItem[] Value);

        private record DatabaseListOutputItem(string Name);
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

        public async Task<IEnumerable<string>> GetDatabaseNamesAsync(CancellationToken ct = default)
        {
            var tracerTimer = new TracerTimer(_tracer);

            _tracer.WriteLine(true, "Get Database names start");

            using (var client = await CreateHttpClient(ct))
            {
                ct = CancellationTokenHelper.MergeCancellationToken(ct, TIMEOUT);

                var apiUrl = $"https://management.azure.com{_clusterId}/databases?api-version=2021-01-01";
                var response = await client.GetAsync(apiUrl, ct);

                _tracer.WriteLine(true, "Database listed");

                var responseText =
                    await response.Content.ReadAsStringAsync(ct);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new InvalidOperationException(
                        $"Database listing failed on cluster ID {_clusterId} "
                        + $"with status code '{response.StatusCode}' "
                        + $"and payload '{responseText}'");
                }
                else
                {
                    var list = JsonSerializer.Deserialize<DatabaseListOutput>(
                        responseText,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (list == null || list.Value == null)
                    {
                        throw new InvalidOperationException(
                            $"Database listing failed on cluster ID {_clusterId} "
                            + $"; can't understand payload '{responseText}'");
                    }

                    var names = list
                        .Value
                        .Select(v => v.Name.Split('/'))
                        .Where(s => s.Length == 2)
                        .Select(s => s[1])
                        .ToImmutableArray();

                    return names;
                }
            }
        }

        public async Task CreateDatabaseAsync(string dbName, CancellationToken ct = default)
        {
            var tracerTimer = new TracerTimer(_tracer);

            _tracer.WriteLine(true, "Create Database start");
            _tracer.WriteLine(true, $"Database name:  '{dbName}'");

            using (var client = await CreateHttpClient(ct))
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

                if (response.StatusCode != HttpStatusCode.OK
                    && response.StatusCode != HttpStatusCode.Created
                    && response.StatusCode != HttpStatusCode.Accepted)
                {
                    throw new InvalidOperationException(
                        $"Database '{dbName}' creation failed on cluster ID {_clusterId} "
                        + $"with status code '{response.StatusCode}' "
                        + $"and payload '{responseText}'");
                }
            }
        }

        public async Task DeleteDatabaseAsync(string dbName, CancellationToken ct = default)
        {
            var tracerTimer = new TracerTimer(_tracer);

            _tracer.WriteLine(true, "Delete Database start");
            _tracer.WriteLine(true, $"Database name:  '{dbName}'");

            using (var client = await CreateHttpClient(ct))
            {
                ct = CancellationTokenHelper.MergeCancellationToken(ct, TIMEOUT);

                var apiUrl = $"https://management.azure.com{_clusterId}/databases/{dbName}?api-version=2021-01-01";
                var response = await client.DeleteAsync(apiUrl, ct);

                _tracer.WriteLine(true, "Database deleted");

                var responseText =
                    await response.Content.ReadAsStringAsync(ct);

                if (response.StatusCode != HttpStatusCode.OK
                    && response.StatusCode != HttpStatusCode.Accepted)
                {
                    throw new InvalidOperationException(
                        $"Database '{dbName}' deletion failed on cluster ID {_clusterId} "
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