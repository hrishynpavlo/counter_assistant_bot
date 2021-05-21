﻿using Microsoft.Extensions.Configuration;

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
                TelegramBotAccessToken = configuration.GetValue<string>("telegram:token"),
                MongoHost = configuration.GetValue<string>("mongo:host"),
                MongoDatabase = configuration.GetValue<string>("mongo:database"),
                MongoUserCollection = configuration.GetValue<string>("mongo:collection:users"),
                MongoCounterCollection = configuration.GetValue<string>("mongo:collection:counters")
            };
        }
    }
}
