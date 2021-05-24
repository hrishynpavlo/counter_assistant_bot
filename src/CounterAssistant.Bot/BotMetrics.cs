using App.Metrics;
using App.Metrics.Counter;

namespace CounterAssistant.Bot
{
    public static class BotMetrics
    {
        public static CounterOptions RecievedMessages => new CounterOptions 
        {
            Name = "bot_recieved_messages",
            MeasurementUnit = Unit.Requests
        };
    }
}
