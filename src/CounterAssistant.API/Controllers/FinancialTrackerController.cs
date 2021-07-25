using CounterAssistant.DataAccess;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CounterAssistant.API.Controllers
{
    [ApiController]
    [Route("api/financial-tracker")]
    public class FinancialTrackerController : ControllerBase
    {
        private readonly IAsyncRepository<MonobankTransaction> _repository;
        private readonly ILogger<FinancialTrackerController> _logger;
        private readonly IMongoCollection<SpendeeRecord> _db;

        public FinancialTrackerController(ILogger<FinancialTrackerController> logger, IAsyncRepository<MonobankTransaction> repository, IMongoCollection<SpendeeRecord> db)
        {
            _repository = repository;
            _logger = logger;
            _db = db;
        }

        [HttpPost("spendee-import")]
        public async Task<IActionResult> ImportSpendee([FromForm] IFormFile data, [FromQuery] DateTime date)
        {
            using var reader = new StreamReader(data.OpenReadStream());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

            var records = new List<SpendeeRecord>();

            foreach(var record in csv.GetRecords<SpendeeRecord>().Where(x => x.Date > date))
            {
                records.Add(record);
            }

            await _db.InsertManyAsync(records);

            return Ok(new { created = records.Count });
        }

        [HttpPost("monobank")]
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

    public class SpendeeRecord
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [Index(0)]
        public DateTime Date { get; set; }

        [Index(3)]
        public string Category { get; set; }

        [Index(4)]
        public decimal Amount { get; set; }

        [Index(6)]
        public string Note { get; set; }
    }
}