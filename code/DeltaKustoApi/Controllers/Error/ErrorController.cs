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

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        public async Task<ErrorOutput> PostAsync(ErrorInput input)
        {
            try
            {
                await TelemetryWriter.WriteTelemetryAsync(input, Request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return new ErrorOutput();
        }
    }
}