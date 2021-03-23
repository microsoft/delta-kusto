using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeltaKustoApi.Controllers.Root
{
    [ApiController]
    [Route("/")]
    public class RootController : ControllerBase
    {
        private readonly ILogger<RootController> _logger;

        public RootController(ILogger<RootController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public RootInfo Get()
        {
            var info = new RootInfo();

            return info;
        }
    }
}
