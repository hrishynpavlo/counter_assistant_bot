using App.Metrics;
using CounterAssistant.Bot.Formatters;
using CounterAssistant.DataAccess;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace CounterAssistant.API.Jobs
{
    [DisallowConcurrentExecution]
    public class ProcessCountersJob : IJob
    {
        private readonly ICounterService _counterService;
        private readonly ITelegramBotClient _botClient;
        private readonly IUserService _userService;
        private readonly IBotMessageFormatter _messageFormatter;
        private readonly ILogger<ProcessCountersJob> _logger;
        private readonly IMetricsRoot _metrics;

        private readonly static MetricTags Tag = new MetricTags("job_name", "process_counter");

        public ProcessCountersJob(ICounterService counterService, ITelegramBotClient botClient, IUserService userService, IBotMessageFormatter messageFormatter, ILogger<ProcessCountersJob> logger, IMetricsRoot metrics)
        {
            _counterService = counterService ?? throw new ArgumentNullException(nameof(counterService));
            _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _messageFormatter = messageFormatter ?? throw new ArgumentNullException(nameof(messageFormatter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Job {job} has started", nameof(ProcessCountersJob));

            try
            {
                var counters = await _counterService.GetCountersForDailyUpdateAsync();
                var users = await _userService.GetUsersByIdsAsync(counters.Keys);

                foreach(var group in counters)
                {
                    if (!users.TryGetValue(group.Key, out var user))
                    {
                        _logger.LogWarning("Couldn't find user {userId}", group.Key);
                        continue;
                    }

                    var message = new StringBuilder();

                    foreach(var counter in group.Value)
                    {
                        counter.Increment();

                        _logger.LogInformation("Counter {counterId} proccesed in background job {job} for user {userId}", counter.Id, nameof(ProcessCountersJob), user.TelegramId);
                        message.AppendLine($"Счётчик <b>{counter.Title.ToUpper()}</b> автоматически увеличен на <b>{counter.Step}</b>.\n<b>{counter.Title.ToUpper()}: {counter.Amount}</b>\n");
                    }

                    await _counterService.BulkUpdateAmountAsync(group.Value);
                    await _botClient.SendTextMessageAsync(user.BotInfo.ChatId, message.ToString(), parseMode: ParseMode.Html, disableNotification: true);
                }

                _metrics.Measure.Counter.Increment(ApiMetrics.SucessfullyFinishedJobs, Tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during execution job {job}", nameof(ProcessCountersJob));
                _metrics.Measure.Counter.Increment(ApiMetrics.FailedJobs, Tag);
                throw;
            }
            finally
            {
                _logger.LogInformation("Job {job} has finished", nameof(ProcessCountersJob));
            }
        }
    }
}
