using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CounterAssistant.Bot
{
    public interface IContextProvider
    {
        Task<ChatContext> GetContext(int userId);
    }

    public class InMemoryContextProvider : IContextProvider
    {
        private readonly ConcurrentDictionary<int, ChatContext> _cache;

        public InMemoryContextProvider()
        {
            _cache = new ConcurrentDictionary<int, ChatContext>();
        }

        public async Task<ChatContext> GetContext(int userId)
        {
            return _cache.GetOrAdd(userId, new ChatContext { UserId = userId });
        }
    }
}
