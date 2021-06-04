using CounterAssistant.DataAccess.DTO;
using CounterAssistant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CounterAssistant.DataAccess
{
    public interface ICounterStore
    {
        //create
        Task CreateCounterAsync(Counter counter, int userId);

        //update
        Task UpdateAsync(Counter counter);
        Task UpdateManyAsync(IEnumerable<Counter> counters);

        //read
        Task<List<Counter>> GetCountersByUserIdAsync(int userId);
        Task<Counter> GetCounterByNameAsync(int userId, string name);
        Task<List<CounterDto>> GetCountersAsync();
        Task<Counter> GetCounterAsync(Guid id);

        //delete
        Task RemoveAsync(Guid id);
    }
}
