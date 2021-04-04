using Microsoft.AspNetCore.Http;
using Serilog.Core;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeltaKustoApi
{
    public class TelemetryWriter
    {
        #region Inner Types
        private class TelemetryInfo<T>
        {
            public string ClientIpAddress { get; set; } = string.Empty;

            public DateTime LogTime { get; set; } = DateTime.Now;

            public T? Input { get; set; }
        }
        #endregion

        private readonly Logger _logger;

        public TelemetryWriter(Logger logger)
        {
            _logger = logger;
        }

        public void PostTelemetry<T>(
            T input,
            HttpRequest request)
        {
            var ipAddress = request.HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? "";
            var telemetry = new TelemetryInfo<T>
            {
                ClientIpAddress = ipAddress,
                Input = input
            };
            var telemetryTextLine = JsonSerializer.Serialize(
                telemetry,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            _logger.Information(telemetryTextLine);
        }
    }
}