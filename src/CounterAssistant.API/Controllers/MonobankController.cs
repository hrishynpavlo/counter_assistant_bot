using CounterAssistant.DataAccess;
using CounterAssistant.DataAccess.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace CounterAssistant.API.Controllers
{
    [ApiController]
    [Route("api/monobank")]
    public class MonobankController : ControllerBase
    {
        private readonly IAsyncRepository<MonobankTransaction> _repository;
        private readonly ILogger<MonobankController> _logger;

        public MonobankController(ILogger<MonobankController> logger, IAsyncRepository<MonobankTransaction> repository)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpPost("recieveTransaction")]
        public async Task<IActionResult> RecieveTransaction([FromBody] MonobankTransaction transaction)
        {
            _logger.LogInformation("MONOBANK TRANSACTION: \n{transaction}", JsonConvert.SerializeObject(transaction, Formatting.Indented));

            try
            {
                await _repository.CreateOneAsync(transaction);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Exception during saving monobank transaction in mongo");
            }

            return Ok();
        }
    }
}