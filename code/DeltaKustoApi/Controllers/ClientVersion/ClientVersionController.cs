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

        public ClientVersionController(
            ClientVersionCacheProxy clientVersionCacheProxy,
            ILogger<ClientVersionController> logger)
        {
            _clientVersionCacheProxy = clientVersionCacheProxy;
            _logger = logger;
        }

        public async Task<ClientVersionOutput> GetAsync(
            [FromQuery]
            string? fromClientVersion)
        {
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
            var newestVersions = await _clientVersionCacheProxy.GetNewestClientVersionsAsync(
                fromClientVersion);

            return newestVersions.FirstOrDefault();
        }
    }
}
