using CounterAssistant.DataAccess;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

namespace CounterAssistant.API.HostedServices
{
    public class CounterProcessorHostedService : IHostedService
    {
        private IDisposable _processor;
        private readonly ILogger<CounterProcessorHostedService> _logger;
        private readonly ICounterStore _store;
        private readonly TelegramBotClient _botClient;
        private readonly IUserStore _userStore;

        private readonly Dictionary<int, long> _userChatMap;

        public CounterProcessorHostedService(ICounterStore store, ILogger<CounterProcessorHostedService> logger, TelegramBotClient botClient, IUserStore userStore)
        {
            _store = store;
            _logger = logger;
            _botClient = botClient;
            _userStore = userStore;
            _userChatMap = new Dictionary<int, long>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Counter proccessor started");
            _processor = Observable.Interval(TimeSpan.FromHours(8)).Subscribe(async _ => 
            {
                var counters = await _store.GetCountersAsync();
                var now = DateTime.UtcNow;
                var filtered = counters.Where(x => (now - x.LastModified.Value).Hours > 23);
                foreach (var counter in filtered)
                {
                    var d = counter.ToDomain();
                    d.Increment();
                    await _store.UpdateAsync(d);

                    if(!_userChatMap.TryGetValue(counter.UserId, out var chatId))
                    {
                        var user = await _userStore.GetUserAsync(counter.UserId);
                        chatId = user.TelegramChatId;
                        _userChatMap.TryAdd(counter.UserId, user.TelegramChatId);
                    }

                    await _botClient.SendTextMessageAsync(chatId, $"Счётчик {counter.Title} автоматически увеличен до {counter.Amount}");
                    _logger.LogInformation("Counter {counterId} proccesed in background for user {userId}", counter.Id, counter.UserId);
                }
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _processor?.Dispose();
            _logger.LogInformation("Counter proccessor stopped");
            return Task.CompletedTask;
        }
    }
}
