using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace delta_kusto
{
    internal static class ApiClient
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

            public string[] AvailableClientVersions { get; set; } = new string[0];
        }

        private class ErrorInput
        {
            public ErrorInput(Exception ex)
            {
                Source = ex.Source ?? string.Empty;
                Exceptions = ExceptionInfo.FromException(ex);
            }

            public ClientInfo ClientInfo { get; set; } = new ClientInfo();

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
                    current = ex.InnerException;
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

        private static readonly string ROOT_URL = ComputeRootUrl();
        private static readonly bool _doApiCalls = ComputeDoApiCalls();

        public static async Task<string[]?> ActivateAsync(CancellationToken ct)
        {
            if (_doApiCalls)
            {
                var output = await PostAsync<ActivationOutput>(
                    "/activation",
                    new
                    {
                        ClientInfo = new ClientInfo()
                    },
                    ct);

                return output?.AvailableClientVersions;
            }
            else
            {
                return null;
            }
        }

        public static async Task<Guid?> RegisterExceptionAsync(Exception ex, CancellationToken ct)
        {
            if (_doApiCalls)
            {
                var output = await PostAsync<ErrorOutput>("/error", new ErrorInput(ex), ct);

                return output?.OperationID;
            }
            else
            {
                return null;
            }
        }

        private static string ComputeRootUrl()
        {
            return Environment.GetEnvironmentVariable("api-url") ?? DEFAULT_ROOT_URL;
        }

        private static bool ComputeDoApiCalls()
        {
            return Environment.GetEnvironmentVariable("disable-api-calls") != "true";
        }

        private static async Task<T?> PostAsync<T>(
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

                using (var client = new HttpClient())
                {
                    var url = new Uri(new Uri(ROOT_URL), urlSuffix);
                    var response = await client.PostAsync(
                        url,
                        new StringContent(bodyText, null, "application/json"),
                        ct);
                    var responseText =
                        await response.Content.ReadAsStringAsync(ct);

                    if (response.StatusCode != HttpStatusCode.OK)
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