using DeltaKustoIntegration;
using DeltaKustoIntegration.Parameterization;
using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace delta_kusto
{
    internal class ApiClient
    {
        #region Inner Types
        private class ApiInfo
        {
            public string ApiVersion { get; set; } = string.Empty;
        }

        private class ClientVersionOutput
        {
            public ApiInfo ApiInfo { get; set; } = new ApiInfo();

            public IImmutableList<string> Versions { get; set; } = ImmutableArray<string>.Empty;
        }
        #endregion

        private const string DEFAULT_ROOT_URL = "https://delta-kusto.azurefd.net/";
        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(5);

        private static readonly string ROOT_URL = ComputeRootUrl();

        private readonly ITracer _tracer;
        private readonly SimpleHttpClientFactory _httpClientFactory;
        private string _sessionId = string.Empty;

        public ApiClient(ITracer tracer, SimpleHttpClientFactory httpClientFactory)
        {
            _tracer = tracer;
            _httpClientFactory = httpClientFactory;
        }

        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        public async Task<IImmutableList<string>?> GetNewestClientVersionsAsync()
        {
            var tokenSource = new CancellationTokenSource(TIMEOUT);
            var ct = tokenSource.Token;

            _tracer.WriteLine(true, "GetNewestClientVersionsAsync - Start");
            try
            {
                using (var client = _httpClientFactory.CreateHttpClient())
                {
                    var url = new Uri(
                        new Uri(ROOT_URL),
                        $"/clientVersion?fromClientVersion={Program.AssemblyVersion}");
                    var response = await client.GetAsync(url, ct);
                    var responseText = await response.Content.ReadAsStringAsync(ct);

                    _tracer.WriteLine(true, "GetNewestClientVersionsAsync - End");
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var output = JsonSerializer.Deserialize<ClientVersionOutput>(
                            responseText,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        if (output != null)
                        {
                            return output.Versions;
                        }
                    }
                }
            }
            catch
            {
                _tracer.WriteLine(true, "GetNewestClientVersionsAsync - Failed");
            }

            return null;
        }

        private static string ComputeRootUrl()
        {
            return Environment.GetEnvironmentVariable("api-url") ?? DEFAULT_ROOT_URL;
        }
    }
}