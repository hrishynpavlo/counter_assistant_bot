using App.Metrics;
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
        private readonly ContextProviderSettings _settings;
        private readonly IMetricsRoot _metrics;
        private readonly ILogger<InMemoryContextProvider> _logger;

        public InMemoryContextProvider(IUserStore userStore, ICounterStore counterStore, IMemoryCache cache, ContextProviderSettings settings, IMetricsRoot metrics, ILogger<InMemoryContextProvider> logger)
        {
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _counterStore = counterStore ?? throw new ArgumentNullException(nameof(counterStore));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ChatContext> GetContextAsync(Message message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var userId = message.From.Id;

            var context = await _cache.GetOrCreateAsync(userId, async cacheOptions => 
            {
                cacheOptions.RegisterPostEvictionCallback(OnDelete);
                cacheOptions.AbsoluteExpirationRelativeToNow = _settings.ExpirationTime;
                cacheOptions.SlidingExpiration = _settings.ProlongationTime;
                
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
                _metrics.Measure.Counter.Increment(BotMetrics.CachedContext);
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

                    _metrics.Measure.Counter.Decrement(BotMetrics.CachedContext);
                }
                else
                {
                    _logger.LogWarning("Entity with {key} doesn't match {type}", key, typeof(ChatContext).Name);
                }
            }
        }
    }
}
