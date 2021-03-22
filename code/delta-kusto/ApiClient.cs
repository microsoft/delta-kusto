using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace delta_kusto
{
    internal static class ApiClient
    {
        private const string ROOT_URL = "http://localhost:8770";

        private static readonly bool _doApiCalls = ComputeDoApiCalls();
        private static readonly object _clientInfo = CreateClientInfo();

        public static async Task ActivateAsync()
        {
            if (_doApiCalls)
            {
                await PostAsync("/activation", new
                {
                    ClientInfo = _clientInfo
                });
            }
        }

        public static async Task RegisterExceptionAsync(Exception ex)
        {
            if (_doApiCalls)
            {
                await PostAsync("/error", new { });
            }
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

        private static async Task PostAsync(string urlSuffix, object telemetry)
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
                        new StringContent(bodyText, null, "application/json"));
                    var responseText = await response.Content.ReadAsStringAsync();

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