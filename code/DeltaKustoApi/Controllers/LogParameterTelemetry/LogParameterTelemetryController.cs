using DeltaKustoApi.Controllers.ClientVersion;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoApi.Controllers.LogParameterTelemetry
{
    [ApiController]
    [Route("[controller]")]
    public class LogParameterTelemetryController : ControllerBase
    {
        private readonly ClientVersionCacheProxy _clientVersionCacheProxy;
        private readonly ILogger<LogParameterTelemetryController> _logger;
        private readonly TelemetryWriter _telemetryWriter;

        public LogParameterTelemetryController(
            ClientVersionCacheProxy clientVersionCacheProxy,
            ILogger<LogParameterTelemetryController> logger,
            TelemetryWriter telemetryWriter)
        {
            _clientVersionCacheProxy = clientVersionCacheProxy;
            _logger = logger;
            _telemetryWriter = telemetryWriter;
        }

        public LogParameterTelemetryOutput Post(LogParameterTelemetryInput input)
        {
            _telemetryWriter.PostTelemetry(input, Request);

            return new LogParameterTelemetryOutput();
        }
    }
}
