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
        Task<ChatContext> GetContextAsync(Message message);
    }

    public class InMemoryContextProvider : IContextProvider
    {
        private readonly IMemoryCache _cache;
        private readonly IUserStore _userStore;
        private readonly ICounterStore _counterStore;
        private readonly ILogger<InMemoryContextProvider> _logger;

        public InMemoryContextProvider(IUserStore userStore, ICounterStore counterStore, IMemoryCache cache, ILogger<InMemoryContextProvider> logger)
        {
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _counterStore = counterStore ?? throw new ArgumentNullException(nameof(counterStore));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ChatContext> GetContextAsync(Message message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var userId = message.From.Id;

            var context = await _cache.GetOrCreateAsync(userId, async cacheOptions => 
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
                cacheOptions.SlidingExpiration = TimeSpan.FromMinutes(3);
                cacheOptions.RegisterPostEvictionCallback(OnDelete);

                var user = await _userStore.GetUserAsync(userId);

                if(user == null)
                {
                    user = Domain.Models.User.Default(userId, message.Chat.Id, message.From.FirstName, message.From.LastName, message.From.Username, BotCommands.START_COMMAND);
                    await _userStore.CreateUserAsync(user);
                }

                Counter counter = null;

                if (user.BotInfo.SelectedCounterId.HasValue)
                {
                    counter = await _counterStore.GetCounterAsync(user.BotInfo.SelectedCounterId.Value);
                }

                return ChatContext.Restore(user, counter);
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
