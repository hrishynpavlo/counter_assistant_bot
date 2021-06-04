using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
            _logger.LogInformation("Called method {method}", nameof(GetVersion));
            return Ok(new 
            { 
                version = AppSettings.AppVersion,
                commitHash = AppSettings.CommitHahs
            });
        }
    }
}

