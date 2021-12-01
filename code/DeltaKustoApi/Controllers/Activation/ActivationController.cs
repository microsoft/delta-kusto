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

namespace DeltaKustoApi.Controllers.Activation
{
    [ApiController]
    [Route("[controller]")]
    public class ActivationController : ControllerBase
    {
        private readonly ClientVersionCacheProxy _clientVersionCacheProxy;
        private readonly ILogger<ActivationController> _logger;
        private readonly TelemetryWriter _telemetryWriter;

        public ActivationController(
            ClientVersionCacheProxy clientVersionCacheProxy,
            ILogger<ActivationController> logger,
            TelemetryWriter telemetryWriter)
        {
            _clientVersionCacheProxy = clientVersionCacheProxy;
            _logger = logger;
            _telemetryWriter = telemetryWriter;
        }

        public async Task<ActivationOutput> PostAsync(ActivationInput input)
        {
            var newestVersions = await _clientVersionCacheProxy.GetNewestClientVersionsAsync(
                input.ClientInfo.ClientVersion);
            var sessionId = Guid.NewGuid().ToString();

            _telemetryWriter.PostTelemetry(
                new
                {
                    SessionId = sessionId,
                    ClientInfo = input.ClientInfo
                },
                Request);

            return new ActivationOutput
            {
                SessionId = sessionId,
                NewestVersions = newestVersions
            };
        }
    }
}
