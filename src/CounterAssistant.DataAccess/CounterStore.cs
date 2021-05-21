using CounterAssistant.DataAccess.DTO;
using CounterAssistant.Domain.Models;
using Microsoft.Extensions.Logging;
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
            var update = Builders<CounterDto>.Update.Set(x => x.Amount, counter.Amount).Set(x => x.LastModified, counter.LastModified);
            await _db.UpdateOneAsync(x => x.Id == counter.Id, update);
        }

        public async Task<List<CounterDto>> GetCountersAsync()
        {
            //var cursor = await _db.FindAsync(x => x.LastModified.HasValue && (DateTime.UtcNow - x.LastModified.Value).Minutes > x.Step);
            var cursor = await _db.FindAsync(x => true);
            return await cursor.ToListAsync();
        }
    }
}
