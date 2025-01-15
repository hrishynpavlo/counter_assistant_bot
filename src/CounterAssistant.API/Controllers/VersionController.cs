using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CounterAssistant.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class VersionController : ControllerBase
    {
        private static readonly string Report = System.IO.File.Exists("Summary.txt") ? System.IO.File.ReadAllText("Summary.txt") : "No data";
        private readonly ILogger<VersionController> _logger;

        public VersionController(ILogger<VersionController> logger)
        {
            _logger = logger;
        }

        [HttpGet("version")]
        public ActionResult GetVersion()
        {
            _logger.LogInformation("Called method {method}", nameof(GetVersion));
            return Ok(new 
            { 
                version = AppSettings.AppVersion,
                commitHash = AppSettings.CommitHash,
                server = AppSettings.Server,
                env = AppSettings.Environment,
                started_at = AppSettings.StartedAt,
                machine_name = AppSettings.MachineName
            });
        }

        [HttpGet("code-coverage")]
        public ActionResult GetCodeCoverage()
        {
            return Ok(Report);
        }
    }
}

