using CounterAssistant.DataAccess;
using CounterAssistant.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace CounterAssistant.Bot
{
    public interface IContextProvider
    {
        Task<ChatContext> GetContext(Message message);
    }

    public class InMemoryContextProvider : IContextProvider
    {
        private readonly IMemoryCache _cache;
        private readonly IUserStore _userStore;
        private readonly ICounterStore _counterStore;
        private readonly ILogger<InMemoryContextProvider> _logger;

        public InMemoryContextProvider(IUserStore userStore, IMemoryCache cache, ICounterStore counterStore, ILogger<InMemoryContextProvider> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _counterStore = counterStore ?? throw new ArgumentNullException(nameof(counterStore));
        }

        public async Task<ChatContext> GetContext(Message message)
        {
            var userId = message.From.Id;

            var context = await _cache.GetOrCreateAsync(userId, async cacheOptions => 
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                cacheOptions.SlidingExpiration = TimeSpan.FromMinutes(3);
                cacheOptions.RegisterPostEvictionCallback(OnDelete);

                var user = await _userStore.GetUserAsync(userId);

                if(user == null)
                {
                    user = new Domain.Models.User();
                    await _userStore.CreateUserAsync(user);
                }

                Counter counter = null;

                if (user.BotInfo.SelectedCounterId.HasValue)
                {
                    counter = await _counterStore.GetCounterAsync(user.BotInfo.SelectedCounterId.Value);
                }

                return ChatContext.FromUser(user, counter);
            });

            return context;
        }

        private async void OnDelete(object key, object value, EvictionReason reason, object state)
        {
            if(reason == EvictionReason.Expired)
            {
                _logger.LogInformation("Entity with {key} is expired", key);

                if(value is ChatContext context)
                {
                    await _userStore.UpdateUserAsync(new Domain.Models.User
                    {
                        TelegramId = context.UserId,
                        BotInfo = new UserBotInfo
                        {
                            LastCommand = context.Command,
                            SelectedCounterId = context.SelectedCounter?.Id,
                            CreateCounterFlowInfo = new CreateCounterFlowInfo 
                            { 
                                State = context.CreateCounterFlow?.State.ToString(), 
                                Args = context.CreateCounterFlow?.Args 
                            }
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("Entity with {key} doesn't match {type}", key, typeof(ChatContext).Name);
                }
            }
        }
    }
}
