using CounterAssistant.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CounterAssistant.API.Controllers
{
    [ApiController]
    [Route("api/counter")]
    public class CounterController : ControllerBase
    {
        private readonly ILogger<CounterController> _logger;
        private readonly ICounterStore _counterStore;

        public CounterController(ILogger<CounterController> logger, ICounterStore counterStore)
        {
            _logger = logger;
            _counterStore = counterStore;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCounter([FromRoute] Guid id, [FromQuery] int amount, [FromQuery]string title, [FromQuery] ushort step)
        {
            var counter = await _counterStore.GetCounterByIdAsync(id);
            counter.Update(amount, step, title);
            await _counterStore.UpdateAsync(counter);

            _logger.LogInformation("Counter {id} updated manually", id);

            return Ok();
        }
    }
}

