using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<ActivationOutput> PostAsync(ActivationInput input)
        {
            try
            {
                _telemetryWriter.PostTelemetry("activations", input, Request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            await Task.CompletedTask;

            return new ActivationOutput();
        }
    }
}
