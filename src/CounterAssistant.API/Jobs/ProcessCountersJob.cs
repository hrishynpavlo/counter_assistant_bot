using CounterAssistant.DataAccess;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;

namespace CounterAssistant.API.Jobs
{
    [DisallowConcurrentExecution]
    public class ProcessCountersJob : IJob
    {
        private readonly ILogger<ProcessCountersJob> _logger;
        private readonly ICounterStore _store;
        private readonly TelegramBotClient _botClient;
        private readonly IUserStore _userStore;

        private readonly Dictionary<int, long> _userChatMap;

        public ProcessCountersJob(ICounterStore store, ILogger<ProcessCountersJob> logger, TelegramBotClient botClient, IUserStore userStore)
        {
            _store = store;
            _logger = logger;
            _botClient = botClient;
            _userStore = userStore;
            _userChatMap = new Dictionary<int, long>();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var counters = await _store.GetCountersAsync();

            foreach (var counter in counters)
            {
                var d = counter.ToDomain();
                d.Increment();
                await _store.UpdateAsync(d);

                if (!_userChatMap.TryGetValue(counter.UserId, out var chatId))
                {
                    var user = await _userStore.GetUserAsync(counter.UserId);
                    chatId = user.TelegramChatId;
                    _userChatMap.TryAdd(counter.UserId, user.TelegramChatId);
                }

                await _botClient.SendTextMessageAsync(chatId, $"Счётчик {counter.Title} автоматически увеличен до {counter.Amount}");
                _logger.LogInformation("Counter {counterId} proccesed in background for user {userId}", counter.Id, counter.UserId);
            }
        }
    }
}
