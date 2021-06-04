using CounterAssistant.DataAccess.DTO;
using CounterAssistant.Domain.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CounterAssistant.DataAccess
{
    public class CounterStore : ICounterStore
    {
        private readonly IMongoCollection<CounterDto> _db;
        private readonly ILogger<CounterStore> _logger;

        public CounterStore(IMongoCollection<CounterDto> db, ILogger<CounterStore> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task CreateCounterAsync(Counter counter, int userId)
        {
            await _db.InsertOneAsync(CounterDto.FromDomain(counter, userId));
        }

        public async Task<Counter> GetCounterByNameAsync(int userId, string name)
        {
            var counter = await _db.Find(x => x.UserId == userId && x.Title == name).FirstOrDefaultAsync();
            return counter?.ToDomain();
        }

        public async Task<List<Counter>> GetCountersByUserIdAsync(int userId)
        {
            var counters = await _db.Find(x => x.UserId == userId).ToListAsync();
            return counters.Select(x => x.ToDomain()).ToList();
        }

        public async Task UpdateAsync(Counter counter)
        {
            var update = Builders<CounterDto>.Update
                .Set(x => x.Amount, counter.Amount)
                .Set(x => x.LastModifiedAt, counter.LastModifiedAt)
                .Set(x => x.Step, counter.Step)
                .Set(x => x.Title, counter.Title);

            await _db.UpdateOneAsync(x => x.Id == counter.Id, update);
        }

        public async Task<List<CounterDto>> GetCountersAsync()
        {
            FilterDefinition<CounterDto> filter = 
                new BsonDocument { 
                    { 
                        "lastModifiedAt", 
                        new BsonDocument { { 
                                "$lt", new BsonDateTime(DateTime.UtcNow.AddDays(-1).AddMinutes(1)) } }
                    },
                    { "isManual", false } 
                };

            var counters = await _db.Find(filter).ToListAsync();

            return counters;
        }

        public async Task UpdateManyAsync(IEnumerable<Counter> counters)
        {
            var operations = counters.Select(x => new UpdateOneModel<CounterDto>(
                new FilterDefinitionBuilder<CounterDto>().Eq(c => c.Id, x.Id),
                new UpdateDefinitionBuilder<CounterDto>().Set(c => c.Amount, x.Amount).Set(c => c.LastModifiedAt, x.LastModifiedAt)
             ));

            var result = await _db.BulkWriteAsync(operations);

            _logger.LogInformation("Operation {op}: requested updates: {total}. Matched total: {matched}, modified total: {modified}", "counterBulkUpdate", counters.Count(), result.MatchedCount, result.ModifiedCount);
        }

        public async Task<Counter> GetCounterAsync(Guid id)
        {
            var counter = await _db.Find(x => x.Id == id).FirstOrDefaultAsync();
            return counter?.ToDomain();
        }

        public async Task RemoveAsync(Guid id)
        {
            var result = await _db.DeleteOneAsync(x => x.Id == id);
            _logger.LogInformation("counter {id} {operation} deleted, the number of removed counters: {number}", id, result.IsAcknowledged ? "was" : "wasn't", result.DeletedCount);
        }
    }
}
