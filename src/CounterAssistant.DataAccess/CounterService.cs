using CounterAssistant.DataAccess.DTO;
using CounterAssistant.Domain.Models;
using System.Threading.Tasks;
using System;
using MongoDB.Driver;
using System.Diagnostics.CodeAnalysis;

namespace CounterAssistant.DataAccess
{
    [ExcludeFromCodeCoverage(Justification = "mock up, not using yet")]
    public class CounterService
    {
        private readonly AsyncRepository<CounterDto> _repository;
        
        public CounterService(AsyncRepository<CounterDto> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task CreateAsync(Counter counter, int userId)
        {
            var dbCounter = CounterDto.FromDomain(counter, userId);
            await _repository.CreateOneAsync(dbCounter);
        }

        public async Task RemoveAsync(Guid id)
        {
            var filter = Builders<CounterDto>.Filter.Eq(x => x.Id, id);
            await _repository.RemoveOneAsync(filter);
        }
    }
}
