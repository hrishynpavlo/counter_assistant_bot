using App.Metrics;
using CounterAssistant.DataAccess;
using CounterAssistant.Domain.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace CounterAssistant.Bot
{
    public interface IContextProvider
    {
        Task<ChatContext> GetContextAsync(BotRequest request);
    }

    public class InMemoryContextProvider : IContextProvider
    {
        private readonly IMemoryCache _cache;
        private readonly IUserService _userService;
        private readonly ICounterService _counterService;
        private readonly ContextProviderSettings _settings;
        private readonly IMetricsRoot _metrics;
        private readonly ILogger<InMemoryContextProvider> _logger;

        public InMemoryContextProvider(IUserService userService, ICounterService counterService, IMemoryCache cache, ContextProviderSettings settings, IMetricsRoot metrics, ILogger<InMemoryContextProvider> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _counterService = counterService ?? throw new ArgumentNullException(nameof(counterService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ChatContext> GetContextAsync(BotRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var userId = request.UserId;

            var context = await _cache.GetOrCreateAsync(userId, async cacheOptions => 
            {
                cacheOptions.RegisterPostEvictionCallback(OnDelete);
                cacheOptions.AbsoluteExpirationRelativeToNow = _settings.ExpirationTime;
                cacheOptions.SlidingExpiration = _settings.ProlongationTime;
                
                var user = await _userService.GetUserByIdAsync(userId);

                if(user == null)
                {
                    user = User.Default(userId, request.ChatId, request.FirstName, request.LastName, request.UserName, BotCommands.START_COMMAND);
                    await _userService.CreateAsync(user);
                }

                Counter counter = null;

                if (user.BotInfo.SelectedCounterId.HasValue)
                {
                    counter = await _counterService.GetCounterByIdAsync(user.BotInfo.SelectedCounterId.Value);
                }
                _metrics.Measure.Counter.Increment(BotMetrics.CachedContext);
                return ChatContext.Restore(user, counter);
            });

            return context;
        }

        [ExcludeFromCodeCoverage(Justification = "it's impossible to test expiration in unit tests")]
        private async void OnDelete(object key, object value, EvictionReason reason, object state)
        {
            if(reason == EvictionReason.Expired)
            {
                _logger.LogInformation("Entity with {key} is expired", key);

                if(value is ChatContext context)
                {
                    await _userService.UpdateUserChatInfoAsync(context.UserId, new UserBotInfo
                    {
                        LastCommand = context.Command,
                        SelectedCounterId = context.SelectedCounter?.Id,
                        CreateCounterFlowInfo = new CreateCounterFlowInfo
                        {
                            State = context.CreateCounterFlow?.State.ToString(),
                            Args = context.CreateCounterFlow?.Args
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
