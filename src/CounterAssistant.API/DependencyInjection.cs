using System;
using App.Metrics;
using App.Metrics.Formatters.Prometheus;
using CounterAssistant.API.HealthChecks;
using CounterAssistant.API.Jobs;
using CounterAssistant.Bot;
using CounterAssistant.Bot.Formatters;
using CounterAssistant.DataAccess;
using CounterAssistant.DataAccess.DTO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Quartz;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using App.Metrics.Reporting.InfluxDB;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Telegram.Bot;

namespace CounterAssistant.API
{
    [ExcludeFromCodeCoverage]
    public static class DependencyInjection
    {
        public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration, AppSettings appSettings)
        {
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton(appSettings);
            return services;
        }

        public static IServiceCollection AddMongoDbPersistance(this IServiceCollection services)
        {
            services.AddSingleton<IMongoDatabase>(sp =>
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                var pack = new ConventionPack { new CamelCaseElementNameConvention() };

                ConventionRegistry.Register(
                    "CamelCaseConvention",
                    pack,
                    t => true);

                //important: map csuuid as uuid 
                BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
                var client = new MongoClient(appSettings.Mongo.Host);
                return client.GetDatabase(appSettings.Mongo.Database);
            });
            
            services.AddSingleton<IMongoCollection<UserDto>>(sp => 
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                var mongo = sp.GetRequiredService<IMongoDatabase>();
                return mongo.GetCollection<UserDto>(appSettings.Mongo.UserCollection);
            });
            
            services.AddSingleton<IMongoCollection<CounterDto>>(sp =>
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                var mongo = sp.GetRequiredService<IMongoDatabase>();
                var collection = mongo.GetCollection<CounterDto>(appSettings.Mongo.CounterCollection);

                var builder = Builders<CounterDto>.IndexKeys;
                var lastModifiedIndex = new CreateIndexModel<CounterDto>(builder.Ascending(x => x.LastModifiedAt));
                var isManulIndex = new CreateIndexModel<CounterDto>(builder.Ascending(x => x.IsManual));

                collection.Indexes.CreateMany(new[] { lastModifiedIndex, isManulIndex });

                return collection;
            });
            
            services.AddSingleton(typeof(IAsyncRepository<>), typeof(AsyncRepository<>));
            services.AddSingleton<ICounterService, CounterService>();
            services.AddSingleton<IUserService, UserService>();

            return services;
        }

        public static IServiceCollection AddTelegramChatContextProvider(this IServiceCollection services)
        {
            services.AddSingleton<ContextProviderSettings>(sp =>
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                return new ContextProviderSettings
                {
                    ExpirationTime = appSettings.InMemoryCache.CacheExpirationTime,
                    ProlongationTime = appSettings.InMemoryCache.CacheProlongationTime
                };
            });
            services.AddSingleton<IContextProvider, InMemoryContextProvider>();
            return services;
        }

        public static IServiceCollection AddTelegramBot(this IServiceCollection services)
        {
            services.AddSingleton<ITelegramBotClient>(sp =>
            {
                var appSettings = sp.GetRequiredService<AppSettings>();
                return new TelegramBotClient(appSettings.Telegram.Token);
            });
            
            services.AddHostedService<BotService>();

            services.AddQuartz(options => 
            {
                const string jobName = "daily_counter_processing";
                var jobKey = new JobKey(jobName);

                options.AddJob<ProcessCountersJob>(jobKey);

                options.AddTrigger(o =>
                {
                    o.ForJob(jobKey)
                        .StartNow();
                });

                options.AddTrigger(o => 
                {
                    o.ForJob(jobKey)
                        .WithCronSchedule("0 5 0 ? * *");
                });
            });
            
            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });
            
            services.AddSingleton<IBotMessageFormatter, BotMessageFormatter>();

            return services;
        }

        public static IServiceCollection AddMetrics(this IServiceCollection services, AppSettings appSettings)
        {
            var metricsBuilder = new MetricsBuilder()
                .Configuration
                .Configure(options => 
                {
                    options.GlobalTags["env"] = AppSettings.Environment;
                    options.GlobalTags["server"] = AppSettings.Server;
                    options.GlobalTags["app"] = AppSettings.AppName;
                    options.GlobalTags["commit_hash"] = AppSettings.CommitHash;
                    options.GlobalTags["machine_name"] = AppSettings.MachineName;
                })
                .OutputMetrics
                .AsPrometheusPlainText();

            if (appSettings.Metrics.Enabled)
            {
                metricsBuilder.Report.ToInfluxDb(options =>
                {
                    options.FlushInterval = TimeSpan.FromSeconds(30);
                    options.InfluxDb = new InfluxDbOptions
                    {
                        BaseUri = new Uri(appSettings.Metrics.InfluxHost),
                        Database = appSettings.Metrics.InfluxDatabase
                    };
                });
            }
                
            var metrics = metricsBuilder.Build();

            services.AddMetrics(metrics);
            services.AddMetricsEndpoints(options => 
            {
                options.MetricsEndpointEnabled = true;
                options.MetricsEndpointOutputFormatter = metrics.OutputMetricsFormatters.OfType<MetricsPrometheusTextOutputFormatter>().First();
                options.MetricsTextEndpointEnabled = false;
                options.EnvironmentInfoEndpointEnabled = false;
            });
            
            return services;
        }

        public static IServiceCollection AddInMemoryCache(this IServiceCollection services)
        {
            services.AddMemoryCache();
            return services;
        }

        public static IServiceCollection AddHealthChecks(this IServiceCollection services, AppSettings appSettings)
        {
            services.AddHealthChecks()
                .AddMongoDb(_ => new MongoClient(appSettings.Mongo.Host),
                    _ => appSettings.Mongo.Database, 
                    tags: ["database", "mongodb"], timeout: TimeSpan.FromSeconds(5))
                .AddTelegramBot();
            
            return services;
        }
    }
}
