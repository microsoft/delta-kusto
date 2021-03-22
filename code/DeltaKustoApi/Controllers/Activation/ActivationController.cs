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

        public ActivationController(ILogger<ActivationController> logger)
        {
            _logger = logger;
        }
    }
}
