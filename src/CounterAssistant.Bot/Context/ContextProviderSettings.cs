using System;

namespace CounterAssistant.Bot
{
    public class ContextProviderSettings
    {
        public TimeSpan ExpirationTime { get; set; }
        public TimeSpan ProlongationTime { get; set; }
    }
}
