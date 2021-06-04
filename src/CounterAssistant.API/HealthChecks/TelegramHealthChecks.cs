using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

namespace CounterAssistant.API.HealthChecks
{
    public class TelegramHealthChecks : IHealthCheck
    {
        private readonly ITelegramBotClient _bot;
        private readonly ILogger<TelegramHealthChecks> _logger;

        public TelegramHealthChecks(ITelegramBotClient bot, ILogger<TelegramHealthChecks> logger)
        {
            _bot = bot ?? throw new ArgumentNullException(nameof(bot));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _bot.GetMeAsync(cancellationToken);
                return new HealthCheckResult(HealthStatus.Healthy);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Telegram bot /me was failed");
                return new HealthCheckResult(HealthStatus.Unhealthy);
            }
        }
    }

    public static class HealthCheckExtesions
    {
        public static IHealthChecksBuilder AddTelegramBot(this IHealthChecksBuilder builder, string name = "telegrambot")
        {
            return builder.AddCheck<TelegramHealthChecks>(name, HealthStatus.Unhealthy, new[] { "telegram", "bot" });
        }
    }
}
