using CounterAssistant.DataAccess.DTO;
using CounterAssistant.Domain.Models;
using System.Threading.Tasks;
using System;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;

namespace CounterAssistant.DataAccess
{
    public interface ICounterService
    {
        Task CreateAsync(Counter counter, int userId);

        Task<Counter> GetCounterByIdAsync(Guid id);
        Task<Counter> GetCounterByBotRequstAsync(int userId, string counterName);
        Task<List<Counter>> GetUserCountersAsync(int userId);
        Task<Dictionary<int, Counter[]>> GetCountersForDailyUpdateAsync();

        Task UpdateAmountAsync(Counter counter);
        Task BulkUpdateAmountAsync(IEnumerable<Counter> counters);

        Task RemoveAsync(Guid id);
    }

    public class CounterService : ICounterService
    {
        private readonly IAsyncRepository<CounterDto> _repository;
        
        public CounterService(IAsyncRepository<CounterDto> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task BulkUpdateAmountAsync(IEnumerable<Counter> counters)
        {
            var update = counters.Select(x => new UpdateOneModel<CounterDto>(
                new FilterDefinitionBuilder<CounterDto>().Eq(c => c.Id, x.Id),
                new UpdateDefinitionBuilder<CounterDto>().Set(c => c.Amount, x.Amount).Set(c => c.LastModifiedAt, x.LastModifiedAt)
             ));

            await _repository.UpdateManyAsync(update);
        }

        public async Task CreateAsync(Counter counter, int userId)
        {
            var dbCounter = CounterDto.FromDomain(counter, userId);
            await _repository.CreateOneAsync(dbCounter);
        }

        public async Task<Counter> GetCounterByBotRequstAsync(int userId, string counterName)
        {
            var userFilter = Builders<CounterDto>.Filter.Eq(x => x.UserId, userId);
            var nameFilter = Builders<CounterDto>.Filter.Eq(x => x.Title, counterName);

            var filter = Builders<CounterDto>.Filter.And(userFilter, nameFilter);
            var dto = await _repository.FindOneAsync(filter);

            return dto?.ToDomain();
        }

        public async Task<Counter> GetCounterByIdAsync(Guid id)
        {
            var filter = Builders<CounterDto>.Filter.Eq(x => x.Id, id);
            var dto = await _repository.FindOneAsync(filter);
            return dto?.ToDomain();
        }

        public async Task<Dictionary<int, Counter[]>> GetCountersForDailyUpdateAsync()
        {
            var dateFilter = Builders<CounterDto>.Filter.Lt(x => x.LastModifiedAt, new BsonDateTime(DateTime.UtcNow.AddDays(-1).AddMinutes(1)));
            var typeFilter = Builders<CounterDto>.Filter.Eq(x => x.IsManual, false);
            var filter = Builders<CounterDto>.Filter.And(dateFilter, typeFilter);

            var result = await _repository.FindManyAsync(filter);

            return result.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => x.Select(dto => dto.ToDomain()).ToArray());
        }

        public async Task<List<Counter>> GetUserCountersAsync(int userId)
        {
            var filter = Builders<CounterDto>.Filter.Eq(x => x.UserId, userId);
            var dtos = await _repository.FindManyAsync(filter);
            return dtos.Select(dto => dto.ToDomain()).ToList();
        }

        public async Task RemoveAsync(Guid id)
        {
            var filter = Builders<CounterDto>.Filter.Eq(x => x.Id, id);
            await _repository.RemoveOneAsync(filter);
        }

        public async Task UpdateAmountAsync(Counter counter)
        {
            var filter = Builders<CounterDto>.Filter.Eq(x => x.Id, counter.Id);
            var update = Builders<CounterDto>.Update
                .Set(x => x.LastModifiedAt, counter.LastModifiedAt)
                .Set(x => x.Amount, counter.Amount);

            await _repository.UpdateOneAsync(filter, update);
        }
    }
}
