using App.Metrics;
using CounterAssistant.DataAccess;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace CounterAssistant.API.Jobs
{
    [DisallowConcurrentExecution]
    public class ProcessCountersJob : IJob
    {
        private readonly ICounterStore _counterStore;
        private readonly ITelegramBotClient _botClient;
        private readonly IUserStore _userStore;
        private readonly ILogger<ProcessCountersJob> _logger;
        private readonly IMetricsRoot _metrics;

        private readonly static MetricTags Tag = new MetricTags("job_name", "process_counter");

        public ProcessCountersJob(ICounterStore counterStore, ITelegramBotClient botClient, IUserStore userStore, ILogger<ProcessCountersJob> logger, IMetricsRoot metrics)
        {
            _counterStore = counterStore ?? throw new ArgumentNullException(nameof(counterStore));
            _botClient = botClient ?? throw new ArgumentNullException(nameof(botClient));
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Job {job} has started", nameof(ProcessCountersJob));

            try
            {
                var counters = await _counterStore.GetCountersAsync();
                var userIds = counters.Select(x => x.UserId).Distinct().ToArray();
                var users = (await _userStore.GetUsersById(userIds)).ToDictionary(x => x.TelegramId);

                foreach (var group in counters.GroupBy(c => c.UserId))
                {
                    if(!users.TryGetValue(group.Key, out var user))
                    {
                        _logger.LogWarning("Couldn't find user {userId}", group.Key);
                        continue;
                    }

                    var message = new StringBuilder();
                    var domains = group.Select(x => x.ToDomain()).ToArray();

                    foreach (var domain in domains)
                    {
                        domain.Increment();

                        _logger.LogInformation("Counter {counterId} proccesed in background job {job} for user {userId}", domain.Id, nameof(ProcessCountersJob), user.TelegramId);
                        message.AppendLine($"Счётчик <b>{domain.Title.ToUpper()}</b> автоматически увеличен на <b>{domain.Step}</b>.\n<b>{domain.Title.ToUpper()} = {domain.Amount}</b>");
                    }

                    await _counterStore.UpdateManyAsync(domains);
                    await _botClient.SendTextMessageAsync(user.TelegramChatId, message.ToString(), parseMode: ParseMode.Html, disableNotification: true);
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
