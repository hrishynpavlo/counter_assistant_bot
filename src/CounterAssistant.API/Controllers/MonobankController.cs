using CounterAssistant.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
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

    public class MonobankTransaction
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Type { get; set; }
        public TransactionData Data { get; set; }
    }

    public class TransactionData
    {
        public string Account { get; set; }
        public StatementItem StatementItem { get; set; }
    }

    public class StatementItem
    {
        public string Id { get; set; }
        public long Time { get; set; }
        public string Description { get; set; }
        public int Mcc { get; set; }
        public bool Hold { get; set; }
        public long Amount { get; set; }
        public long OperationAmount { get; set; }
        public int CurrencyCode { get; set; }
        public long CommissionRate { get; set; }
        public long CashbackAmount { get; set; }
        public long Balance { get; set; }
        public string Comment { get; set; }
        public string ReceiptId { get; set; }
        public string CounterEdrpou { get; set; }
        public string CounterIban { get; set; }
    }
}