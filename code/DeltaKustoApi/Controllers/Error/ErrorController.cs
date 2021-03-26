using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeltaKustoApi.Controllers.Error
{
    [ApiController]
    [Route("[controller]")]
    public class ErrorController : ControllerBase
    {
        private readonly ILogger<ErrorController> _logger;
        private readonly TelemetryWriter _telemetryWriter;

        public ErrorController(
            ILogger<ErrorController> logger,
            TelemetryWriter telemetryWriter)
        {
            _logger = logger;
            _telemetryWriter = telemetryWriter;
        }

        public async Task<ErrorOutput> PostAsync(ErrorInput input)
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

            return new ErrorOutput();
        }
    }
}