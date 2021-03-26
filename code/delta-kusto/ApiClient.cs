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
        private const string ROOT_URL = "http://localhost:8770";

        private static readonly bool _doApiCalls = ComputeDoApiCalls();
        private static readonly object _clientInfo = CreateClientInfo();

        public static async Task<int> ActivateAsync(CancellationToken ct)
        {
            if (_doApiCalls)
            {
                await PostAsync(
                    "/activation",
                    new
                    {
                        ClientInfo = _clientInfo
                    },
                    ct);

                return 1;
            }
            else
            {
                return 0;
            }
        }

        public static async Task RegisterExceptionAsync(Exception ex, CancellationToken ct)
        {
            if (_doApiCalls)
            {
                await PostAsync(
                    "/error", new
                    {
                        ClientInfo = _clientInfo,
                        Source = ex.Source ?? string.Empty,
                        Exceptions = CreateExceptions(ex).ToArray()
                    },
                    ct);
            }
        }

        private static IEnumerable<object> CreateExceptions(Exception ex)
        {
            var head = new
            {
                Message = ex.Message,
                ExceptionType = ex.GetType().FullName,
                StackTrace = ex.StackTrace
            };

            return ex.InnerException != null
                ? CreateExceptions(ex.InnerException).Prepend(head)
                : new[] { head };
        }

        private static bool ComputeDoApiCalls()
        {
            return Environment.GetEnvironmentVariable("disable-api-calls") != "true";
        }

        private static object CreateClientInfo()
        {
            return new
            {
                ClientVersion = GetAssemblyVersion(),
                OS = Environment.OSVersion.Platform.ToString(),
                OsVersion = Environment.OSVersion.Version.ToString()
            };
        }

        private static string GetAssemblyVersion()
        {
            var versionAttribute = typeof(ApiClient)
                .Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var version = versionAttribute == null
                ? "<VERSION MISSING>"
                : versionAttribute!.InformationalVersion;

            return version;
        }

        private static async Task PostAsync(
            string urlSuffix,
            object telemetry,
            CancellationToken ct)
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
                    }
                }
            }
            catch
            {
            }
        }
    }
}