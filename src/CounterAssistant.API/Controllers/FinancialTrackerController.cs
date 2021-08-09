using CounterAssistant.DataAccess;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CounterAssistant.API.Controllers
{
    [ApiController]
    [Route("api/financial-tracker")]
    public class FinancialTrackerController : ControllerBase
    {
        private readonly IFinancialTrackerImporter _importer;
        private readonly ILogger<FinancialTrackerController> _logger;

        public FinancialTrackerController(IFinancialTrackerImporter importer, ILogger<FinancialTrackerController> logger)
        {
            _importer = importer ?? throw new ArgumentNullException(nameof(importer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("spendee-import")]
        public async Task<IActionResult> ImportSpendee(IFormFile data, [FromHeader(Name = "X-TELEGRAM-USER-ID")] int telegramUserId)
        {
            var created = await _importer.ImportAsync(data.OpenReadStream(), telegramUserId);

            return Ok(new { telegramUserId, created });
        }
    }
}