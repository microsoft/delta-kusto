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

        public LogParameterTelemetryController(
            ClientVersionCacheProxy clientVersionCacheProxy,
            ILogger<LogParameterTelemetryController> logger)
        {
            _clientVersionCacheProxy = clientVersionCacheProxy;
            _logger = logger;
        }

        public LogParameterTelemetryOutput Post(LogParameterTelemetryInput input)
        {
            return new LogParameterTelemetryOutput();
        }
    }
}
