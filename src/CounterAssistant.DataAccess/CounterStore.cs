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
            var cursor = await _db.FindAsync(x => x.UserId == userId && x.Title == name);
            var counter = await cursor.FirstOrDefaultAsync();
            return counter?.ToDomain();
        }

        public async Task<List<Counter>> GetCountersByUserIdAsync(int userId)
        {
            var cursor = await _db.FindAsync(x => x.UserId == userId);
            var counters = await cursor.ToListAsync();
            return counters.Select(x => x.ToDomain()).ToList();
        }

        public async Task UpdateAsync(Counter counter)
        {
            var update = Builders<CounterDto>.Update.Set(x => x.Amount, counter.Amount).Set(x => x.LastModified, counter.LastModified).Set(x => x.Step, counter.Step).Set(x => x.Title, counter.Title);
            await _db.UpdateOneAsync(x => x.Id == counter.Id, update);
        }

        public async Task<List<CounterDto>> GetCountersAsync()
        {
            var lastModifiedExpression = new BsonDocument { { "$ifNull", new BsonArray(new[] { "$lastModified", "$created" }) } };
            var projection = new BsonDocument { { "_id", "$_id" }, { "lastModifiedFilter", lastModifiedExpression }, { "step", "$step" }, { "amount", "$amount" }, { "userId", "$userId" }, { "title", "$title" }, { "created", "$created" }, { "lastModified", "$lastModified" } };
            var filter = new BsonDocument { { "lastModifiedFilter", new BsonDocument { { "$lt", new BsonDateTime(DateTime.UtcNow.AddDays(-1)) } } } };

            var bson = await _db.Aggregate().Project(projection).Match(filter).ToListAsync();
            var result = bson.Select(x => new CounterDto 
            {
                Id = x.GetValue("_id").AsGuid,
                Amount = x.GetValue("amount").AsInt32,
                Step = (ushort)x.GetValue("step").AsInt32,
                UserId = x.GetValue("userId").AsInt32,
                LastModified = x.GetValue("lastModified").ToNullableUniversalTime(),
                Created = x.GetValue("created").ToUniversalTime(),
                Title = x.GetValue("title").AsString
            }).ToList();

            return result;
        }

        public async Task<Counter> GetCounterByIdAsync(Guid id)
        {
            var counter = await _db.Find(x => x.Id == id).FirstOrDefaultAsync();
            return counter?.ToDomain();
        }
    }
}
