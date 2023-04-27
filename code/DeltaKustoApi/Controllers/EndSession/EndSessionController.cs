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

namespace DeltaKustoApi.Controllers.EndSession
{
    [ApiController]
    [Route("[controller]")]
    public class EndSessionController : ControllerBase
    {
        private readonly ClientVersionCacheProxy _clientVersionCacheProxy;
        private readonly ILogger<EndSessionController> _logger;

        public EndSessionController(
            ClientVersionCacheProxy clientVersionCacheProxy,
            ILogger<EndSessionController> logger)
        {
            _clientVersionCacheProxy = clientVersionCacheProxy;
            _logger = logger;
        }

        public EndSessionOutput Post(EndSessionInput input)
        {
            return new EndSessionOutput();
        }
    }
}
