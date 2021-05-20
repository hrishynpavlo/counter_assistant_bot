using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CounterAssistant.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class VersionController : ControllerBase
    {
        private readonly ILogger<VersionController> _logger;

        public VersionController(ILogger<VersionController> logger)
        {
            _logger = logger;
        }

        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            return Ok(new { version = "v1" });
        }
    }
}
