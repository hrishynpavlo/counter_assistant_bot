using CounterAssistant.Bot;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CounterAssistant.API.HostedServices
{
    public class BotHostedService : IHostedService
    {
        private readonly BotService _bot;

        public BotHostedService(BotService bot)
        {
            _bot = bot;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _bot.StartAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _bot.Dispose();
            return Task.CompletedTask;
        }
    }
}
