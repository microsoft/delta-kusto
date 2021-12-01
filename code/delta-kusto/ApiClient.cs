using DeltaKustoIntegration;
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
        private class ClientInfo
        {
            public string ClientVersion { get; set; } = Program.AssemblyVersion;

            public string OS { get; set; } = Environment.OSVersion.Platform.ToString();

            public string OsVersion { get; set; } = Environment.OSVersion.VersionString;
        }

        private class ApiInfo
        {
            public string ApiVersion { get; set; } = string.Empty;
        }

        private class ActivationOutput
        {
            public ApiInfo ApiInfo { get; set; } = new ApiInfo();

            public string SessionId { get; set; } = string.Empty;

            public IImmutableList<string> NewestVersions { get; set; } = ImmutableArray<string>.Empty;
        }

        private class ErrorInput
        {
            public ErrorInput(Exception ex)
            {
                Source = ex.Source ?? string.Empty;
                Exceptions = ExceptionInfo.FromException(ex);
            }

            public ClientInfo ClientInfo { get; set; } = new ClientInfo();

            public string SessionId { get; set; } = string.Empty;

            public string Source { get; set; }

            public ExceptionInfo[] Exceptions { get; set; }
        }

        private class ExceptionInfo
        {
            private ExceptionInfo(Exception ex)
            {
                Message = ex.Message;
                ExceptionType = ex.GetType().FullName ?? string.Empty;
                StackTrace = ex.StackTrace ?? string.Empty;
            }

            internal static ExceptionInfo[] FromException(Exception ex)
            {
                var list = new List<ExceptionInfo>();
                Exception? current = ex;

                while (current != null)
                {
                    list.Add(new ExceptionInfo(current));
                    current = current.InnerException;
                }

                return list.ToArray();
            }

            public string Message { get; set; }

            public string ExceptionType { get; set; }

            public string StackTrace { get; set; }
        }

        private class ErrorOutput
        {
            public ApiInfo ApiInfo { get; set; } = new ApiInfo();

            public Guid OperationID { get; set; } = Guid.NewGuid();
        }
        #endregion

        private const string DEFAULT_ROOT_URL = "https://delta-kusto.azurefd.net/";
        private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(10);

        private static readonly string ROOT_URL = ComputeRootUrl();
        private static readonly bool _doApiCalls = ComputeDoApiCalls();

        private readonly ITracer _tracer;
        private readonly SimpleHttpClientFactory _httpClientFactory;

        public ApiClient(ITracer tracer, SimpleHttpClientFactory httpClientFactory)
        {
            _tracer = tracer;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IImmutableList<string>?> ActivateAsync()
        {
            if (_doApiCalls)
            {
                var tokenSource = new CancellationTokenSource(TIMEOUT);
                var ct = tokenSource.Token;

                _tracer.WriteLine(true, "ActivateAsync - Start");
                try
                {
                    var output = await PostAsync<ActivationOutput>(
                        "/activation",
                        new
                        {
                            ClientInfo = new ClientInfo()
                        },
                        ct);

                    _tracer.WriteLine(true, "ActivateAsync - End");

                    if (output != null)
                    {
                        return output.NewestVersions;
                    }
                }
                catch
                {
                    _tracer.WriteLine(true, "ActivateAsync - Failed");
                }
            }

            return null;
        }

        public async Task<Guid?> RegisterExceptionAsync(Exception ex)
        {
            if (_doApiCalls)
            {
                var tokenSource = new CancellationTokenSource(TIMEOUT);
                var ct = tokenSource.Token;

                _tracer.WriteLine(true, "RegisterExceptionAsync - Start");
                try
                {
                    var output = await PostAsync<ErrorOutput>("/error", new ErrorInput(ex), ct);

                    _tracer.WriteLine(true, "RegisterExceptionAsync - End");

                    return output?.OperationID;
                }
                catch
                {
                    _tracer.WriteLine(true, "RegisterExceptionAsync - Failed");
                }
            }

            return null;
        }

        private static string ComputeRootUrl()
        {
            return Environment.GetEnvironmentVariable("api-url") ?? DEFAULT_ROOT_URL;
        }

        private static bool ComputeDoApiCalls()
        {
            return Environment.GetEnvironmentVariable("disable-api-calls") != "true";
        }

        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        private async Task<T?> GetAsync<T>(
            string urlSuffix,
            CancellationToken ct,
            params (string name, string value)[] queryParameters)
            where T : class
        {
            try
            {
                using (var client = _httpClientFactory.CreateHttpClient())
                {
                    var queryParametersText = queryParameters
                        .Select(q => WebUtility.UrlEncode(q.name) + "=" + WebUtility.UrlEncode(q.value));
                    var url = new Uri(new Uri(ROOT_URL), urlSuffix);
                    var urlWithQuery = new Uri(url, "?" + string.Join("&", queryParametersText));

                    var response = await client.GetAsync(
                        urlWithQuery,
                        HttpCompletionOption.ResponseContentRead,
                        ct);
                    var responseText =
                        await response.Content.ReadAsStringAsync(ct);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var output = JsonSerializer.Deserialize<T>(
                            responseText,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        return output!;
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        private async Task<T?> PostAsync<T>(
            string urlSuffix,
            object telemetry,
            CancellationToken ct)
            where T : class
        {
            try
            {
                var bodyText = JsonSerializer.Serialize(
                    telemetry,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                using (var client = _httpClientFactory.CreateHttpClient())
                {
                    var url = new Uri(new Uri(ROOT_URL), urlSuffix);
                    var response = await client.PostAsync(
                        url,
                        new StringContent(bodyText, null, "application/json"),
                        ct);
                    var responseText =
                        await response.Content.ReadAsStringAsync(ct);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var output = JsonSerializer.Deserialize<T>(
                            responseText,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        return output!;
                    }
                }
            }
            catch
            {
            }

            return null;
        }
    }
}