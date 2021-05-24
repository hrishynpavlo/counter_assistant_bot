using Microsoft.Extensions.Configuration;

namespace CounterAssistant.API
{
    public class AppSettings
    {
        public string TelegramBotAccessToken { get; private set; }
        public string MongoHost { get; private set; }
        public string MongoDatabase { get; private set; }
        public string MongoUserCollection { get; private set; }
        public string MongoCounterCollection { get; private set; }

        public static AppSettings FromConfig(IConfiguration configuration)
        {
            return new AppSettings
            {
                TelegramBotAccessToken = configuration.GetValue<string>("telegram:token") ?? configuration.GetValue<string>("TELEGRAM_TOKEN"),
                MongoHost = configuration.GetValue<string>("mongo:host") ?? configuration.GetValue<string>("MONGO_HOST"),
                MongoDatabase = configuration.GetValue<string>("mongo:database") ?? configuration.GetValue<string>("MONGO_DATABASE"),
                MongoUserCollection = configuration.GetValue<string>("mongo:collection:users"),
                MongoCounterCollection = configuration.GetValue<string>("mongo:collection:counters")
            };
        }
    }
}
