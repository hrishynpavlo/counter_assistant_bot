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

        public static CounterOptions Errors => new CounterOptions 
        {
            Name = "bot_total_errors",
            MeasurementUnit = Unit.Errors
        };

        public static CounterOptions StartedChats => new CounterOptions 
        {
            Name = "bot_total_started_chats",
            MeasurementUnit = Unit.Commands
        };

        public static CounterOptions CachedContext => new CounterOptions
        {
            Name = "bot_cached_chat_contexts",
            MeasurementUnit = Unit.Items
        };
    }
}
