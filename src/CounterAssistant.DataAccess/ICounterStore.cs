﻿using CounterAssistant.DataAccess.DTO;
using CounterAssistant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CounterAssistant.DataAccess
{
    public interface ICounterStore
    {
        Task<Counter> GetCounterByIdAsync(Guid id);
        Task<List<Counter>> GetCountersByUserIdAsync(int userId);
        Task<Counter> GetCounterByNameAsync(int userId, string name);
        Task CreateCounterAsync(Counter counter, int userId);
        Task UpdateAsync(Counter counter);
        Task<List<CounterDto>> GetCountersAsync();
    }
}
