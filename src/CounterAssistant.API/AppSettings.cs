using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CounterAssistant.API
{
    [ExcludeFromCodeCoverage]
    public class AppSettings
    {
        public TelegramSettings Telegram { get; private set; }
        public MongoSettings Mongo { get; private set; }
        public InMemoryCacheSettings InMemoryCache { get; private set; }

        public static AppSettings FromConfig(IConfiguration configuration)
        {
            Server = configuration.GetValue("Server", "local");
            Environment = configuration.GetValue("Environment", "local");
            CommitHash = configuration.GetValue("COMMIT_HASH", "local");
            StartedAt = DateTime.UtcNow;
            MachineName = configuration.GetValue("MACHINE_NAME", "local");

            var telegramConfig = configuration.GetSection("Telegram");
            var telegramToken = telegramConfig.GetValue<string>("Token");
            var telegramSettings = new TelegramSettings(telegramToken);
            
            var mongoConfig = configuration.GetSection("Mongo");
            var mongoHost = mongoConfig.GetValue<string>("Host");
            var mongoDatabase = mongoConfig.GetValue<string>("Database");
            var mongoUserCollection = mongoConfig.GetValue<string>("UserCollection");
            var mongoCounterCollection = mongoConfig.GetValue<string>("CounterCollection");
            var mongoSettings = new MongoSettings(mongoHost, mongoDatabase, mongoUserCollection, mongoCounterCollection);
            
            var inMemoryCacheConfig = configuration.GetSection("InMemoryCache");
            var cacheExpirationTime = TimeSpan.FromMinutes(inMemoryCacheConfig.GetValue("ExpirationTimeInMinutes", 30));
            var cacheProlongationTime = TimeSpan.FromMinutes(inMemoryCacheConfig.GetValue("ProlongationTimeInMinutes", 3));
            var inMemoryCacheSettings = new InMemoryCacheSettings(cacheExpirationTime, cacheProlongationTime);
            
            return new AppSettings
            {
                Telegram = telegramSettings,
                Mongo = mongoSettings,
                InMemoryCache = inMemoryCacheSettings
            };
        }

        public static string CommitHash { get; private set; }
        public static string Server { get; private set; }
        public static string Environment { get; private set; }
        public static DateTime StartedAt { get; private set; }
        public static string MachineName { get; private set; }
        public static string AppName => "counter_assistance_bot";
        public static string AppVersion => "v1-beta";
    }

    public record TelegramSettings(string Token);
    public record MongoSettings(string Host, string Database, string UserCollection, string CounterCollection);
    public record InMemoryCacheSettings(TimeSpan CacheExpirationTime, TimeSpan CacheProlongationTime);
}
