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
        private readonly ILogger<ProcessCountersJob> _logger;
        private readonly ICounterStore _store;
        private readonly ITelegramBotClient _botClient;
        private readonly IUserStore _userStore;
        private readonly IMetricsRoot _metrics;

        private readonly static MetricTags Tag = new MetricTags("job_name", "process_counter");

        public ProcessCountersJob(ICounterStore store, ILogger<ProcessCountersJob> logger, ITelegramBotClient botClient, IUserStore userStore, IMetricsRoot metrics)
        {
            _store = store;
            _logger = logger;
            _botClient = botClient;
            _userStore = userStore;
            _metrics = metrics;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Job {job} has started", nameof(ProcessCountersJob));

            try
            {
                var counters = await _store.GetCountersAsync();
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

                    await _store.UpdateManyAsync(domains);
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
