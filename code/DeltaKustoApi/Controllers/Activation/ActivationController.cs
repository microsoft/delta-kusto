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
        private readonly ILogger<ActivationController> _logger;
        private readonly TelemetryWriter _telemetryWriter;

        public ActivationController(
            ILogger<ActivationController> logger,
            TelemetryWriter telemetryWriter)
        {
            _logger = logger;
            _telemetryWriter = telemetryWriter;
        }

        public ActivationOutput Post(ActivationInput input)
        {
            try
            {
                _telemetryWriter.PostTelemetry(input, Request);

                return new ActivationOutput();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                return new ActivationOutput();
            }
        }
    }
}
