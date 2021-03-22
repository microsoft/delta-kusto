using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace DeltaKustoApi
{
    internal class TelemetryWriter
    {
        #region Inner Types
        private class TelemetryInfo<T>
            where T : new()
        {
            public string ClientIpAddress { get; set; } = string.Empty;

            public DateTime LogTime { get; set; } = DateTime.Now;

            public T Input { get; set; } = new T();
        }
        #endregion

        public static Task WriteTelemetryAsync<T>(T input, HttpRequest request)
            where T : new()
        {
            var ipAddress = request.HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? "";
            var telemetry = new TelemetryInfo<T>
            {
                ClientIpAddress = ipAddress,
                Input = input
            };

            throw new NotImplementedException();
        }
    }
}