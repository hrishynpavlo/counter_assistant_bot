using CounterAssistant.DataAccess;
using CounterAssistant.DataAccess.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace CounterAssistant.API.Controllers
{
    [ApiController]
    [Route("api/monobank")]
    public class MonobankController : ControllerBase
    {
        private readonly IPipeline<MonobankTransaction> _reciever;
        private readonly ILogger<MonobankController> _logger;

        public MonobankController(IPipeline<MonobankTransaction> reciever, ILogger<MonobankController> logger)
        {
            _reciever = reciever ?? throw new ArgumentNullException(nameof(reciever));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("recieveTransaction")]
        public IActionResult RecieveTransaction([FromBody] MonobankTransaction transaction)
        {
            if(transaction.Data.StatementItem.Amount < 0)
            {
                _reciever.Recieve(transaction);
            }

            return Ok();
        }
    }
}