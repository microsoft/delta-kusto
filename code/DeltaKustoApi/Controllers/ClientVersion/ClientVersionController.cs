using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoApi.Controllers.ClientVersion
{
    [ApiController]
    [Route("[controller]")]
    public class ClientVersionController : ControllerBase
    {
        private readonly ClientVersionCacheProxy _clientVersionCacheProxy;
        private readonly ILogger<ClientVersionController> _logger;
        private readonly TelemetryWriter _telemetryWriter;

        public ClientVersionController(
            ClientVersionCacheProxy clientVersionCacheProxy,
            ILogger<ClientVersionController> logger,
            TelemetryWriter telemetryWriter)
        {
            _clientVersionCacheProxy = clientVersionCacheProxy;
            _logger = logger;
            _telemetryWriter = telemetryWriter;
        }

        public async Task<ClientVersionOutput> GetAsync(
            [FromQuery]
            string? fromClientVersion)
        {
            _telemetryWriter.PostTelemetry(
                $"clientVersion:  {fromClientVersion}",
                Request);

            var newestVersions = await _clientVersionCacheProxy.GetNewestClientVersionsAsync(
                fromClientVersion);

            return new ClientVersionOutput
            {
                Versions = newestVersions
            };
        }

        [Route("unique")]
        public async Task<string?> GetUniqueAsync(
            [FromQuery]
            string? fromClientVersion)
        {
            _telemetryWriter.PostTelemetry(
                $"clientVersion:  {fromClientVersion}",
                Request);

            var newestVersions = await _clientVersionCacheProxy.GetNewestClientVersionsAsync(
                fromClientVersion);

            return newestVersions.LastOrDefault();
        }
    }
}
