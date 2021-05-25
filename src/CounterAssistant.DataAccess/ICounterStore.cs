using CounterAssistant.DataAccess.DTO;
using CounterAssistant.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CounterAssistant.DataAccess
{
    public interface ICounterStore
    {
        Task<List<Counter>> GetCountersByUserIdAsync(int userId);
        Task<Counter> GetCounterByNameAsync(int userId, string name);
        Task CreateCounterAsync(Counter counter, int userId);
        Task UpdateAsync(Counter counter);
        Task<List<CounterDto>> GetCountersAsync();
        Task UpdateManyAsync(IEnumerable<Counter> counters);
    }
}
