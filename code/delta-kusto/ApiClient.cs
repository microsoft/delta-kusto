using System;
using System.Threading.Tasks;

namespace delta_kusto
{
    internal static class ApiClient
    {
        private const string ROOT_URL = "http://localhost/8770";

        private static readonly bool _doApiCalls = ComputeDoApiCalls();

        public static async Task ActivateAsync()
        {
            if (_doApiCalls)
            {
                await PostAsync("/activations", new { });
            }
        }

        public static async Task RegisterExceptionAsync(Exception ex)
        {
            if (_doApiCalls)
            {
                await PostAsync("/errors", new { });
            }
        }

        private static bool ComputeDoApiCalls()
        {
            return Environment.GetEnvironmentVariable("disable-api-calls") == "true";
        }

        private static Task PostAsync(string urlSuffix, object telemetry)
        {
            throw new NotImplementedException();
        }
    }
}