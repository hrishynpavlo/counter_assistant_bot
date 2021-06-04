using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CounterAssistant.API
{
    [ExcludeFromCodeCoverage]
    public class AppSettings
    {
        public string TelegramBotAccessToken { get; private set; }
        public string MongoHost { get; private set; }
        public string MongoDatabase { get; private set; }
        public string MongoUserCollection { get; private set; }
        public string MongoCounterCollection { get; private set; }
        public TimeSpan CacheExpirationTime { get; private set; }
        public TimeSpan CacheProlongationTime { get; private set; }
        public string Server { get; private set; }
        public string Environment { get; private set; }

        public static AppSettings FromConfig(IConfiguration configuration)
        {
            CommitHahs = configuration.GetValue("COMMIT_HASH", "local");

            return new AppSettings
            {
                TelegramBotAccessToken = configuration.GetValue<string>("telegram:token") ?? configuration.GetValue<string>("TELEGRAM_TOKEN"),
                MongoHost = configuration.GetValue<string>("mongo:host") ?? configuration.GetValue<string>("MONGO_HOST"),
                MongoDatabase = configuration.GetValue<string>("mongo:database") ?? configuration.GetValue<string>("MONGO_DATABASE"),
                MongoUserCollection = configuration.GetValue<string>("mongo:collection:users"),
                MongoCounterCollection = configuration.GetValue<string>("mongo:collection:counters"),
                CacheExpirationTime = TimeSpan.FromMinutes(configuration.GetValue("cache:expirationTimeInMinutes", 30)),
                CacheProlongationTime = TimeSpan.FromMinutes(configuration.GetValue("cache:prolongationTimeInMinutes", 3)),
                Server = configuration.GetValue("SERVER", "local"),
                Environment = configuration.GetValue("ENVIRONMENT", "local")
            };
        }

        public static string CommitHahs { get; private set; }
        public static string AppName => "counter_assistance_bot";
        public static string AppVersion => "v1-beta";
    }
}
