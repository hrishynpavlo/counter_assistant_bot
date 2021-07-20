using App.Metrics;
using App.Metrics.Formatters.Prometheus;
using CounterAssistant.API.Controllers;
using CounterAssistant.API.Extensions;
using CounterAssistant.API.HealthChecks;
using CounterAssistant.API.Jobs;
using CounterAssistant.Bot;
using CounterAssistant.Bot.Formatters;
using CounterAssistant.DataAccess;
using CounterAssistant.DataAccess.DTO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Quartz;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Telegram.Bot;

namespace CounterAssistant.API
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var appSettings = AppSettings.FromConfig(Configuration);

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Counter Assistant Bot API", Version = "v1" });
            });

            services.AddSingleton<IMongoDatabase>(_ => 
            {
                var pack = new ConventionPack();
                pack.Add(new CamelCaseElementNameConvention());

                ConventionRegistry.Register(
                   "CamelCaseConvention",
                   pack,
                   t => true);

                //important: map csuuid as uuid 
                //obsolete: Configure serializers doesn't work
                MongoDefaults.GuidRepresentation = GuidRepresentation.Standard;

                var client = new MongoClient(appSettings.MongoHost);
                return client.GetDatabase(appSettings.MongoDatabase);
            });
            services.AddSingleton<IMongoCollection<MonobankTransaction>>(provider => 
            {
                var mongo = provider.GetService<IMongoDatabase>();
                return mongo.GetCollection<MonobankTransaction>(appSettings.MongoMonobankTransactionCollection);
            });

            services.AddSingleton<IMongoCollection<UserDto>>(provider => 
            {
                var mongo = provider.GetService<IMongoDatabase>();
                return mongo.GetCollection<UserDto>(appSettings.MongoUserCollection);
            });

            services.AddSingleton<IMongoCollection<CounterDto>>(provider =>
            {
                var mongo = provider.GetService<IMongoDatabase>();
                var collection = mongo.GetCollection<CounterDto>(appSettings.MongoCounterCollection);

                var builder = Builders<CounterDto>.IndexKeys;
                var lastModifiedIndex = new CreateIndexModel<CounterDto>(builder.Ascending(x => x.LastModifiedAt));
                var isManulIndex = new CreateIndexModel<CounterDto>(builder.Ascending(x => x.IsManual));

                collection.Indexes.CreateMany(new[] { lastModifiedIndex, isManulIndex });

                return collection;
            });

            services.AddSingleton<ContextProviderSettings>(_ => new ContextProviderSettings 
            { 
                ExpirationTime = appSettings.CacheExpirationTime,
                ProlongationTime = appSettings.CacheProlongationTime 
            });
            services.AddSingleton<IContextProvider, InMemoryContextProvider>();

            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(appSettings.TelegramBotAccessToken));

            services.AddHostedService<BotService>();

            services.AddQuartz(options => 
            {
                options.UseMicrosoftDependencyInjectionJobFactory();

                const string jobName = "daily_counter_processing";
                var jobKey = new JobKey(jobName);

                options.AddJob<ProcessCountersJob>(jobKey);

                options.AddTrigger(options =>
                {
                    options.ForJob(jobKey)
                        .StartNow();
                });

                options.AddTrigger(options => 
                {
                    options.ForJob(jobKey)
                        .WithCronSchedule("0 5 0 ? * *");
                });
            });

            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });

            var metrics = new MetricsBuilder()
                .Configuration
                .Configure(options => 
                {
                    options.GlobalTags["env"] = appSettings.Environment;
                    options.GlobalTags["server"] = appSettings.Server;
                    options.GlobalTags["app"] = AppSettings.AppName;
                    options.GlobalTags["commit_hash"] = AppSettings.CommitHahs;
                })
                .OutputMetrics
                .AsPrometheusPlainText()
                .Build();

            services.AddMetrics(metrics);
            services.AddMetricsEndpoints(options => 
            {
                options.MetricsEndpointEnabled = true;
                options.MetricsEndpointOutputFormatter = metrics.OutputMetricsFormatters.OfType<MetricsPrometheusTextOutputFormatter>().First();
                options.MetricsTextEndpointEnabled = false;
                options.EnvironmentInfoEndpointEnabled = false;
            });

            services.AddMemoryCache();

            services.AddHealthChecks()
                .AddMongoDb(appSettings.MongoHost, tags: new[] { "database", "mongodb" })
                .AddTelegramBot();

            services.AddSingleton(typeof(IAsyncRepository<>), typeof(AsyncRepository<>));
            services.AddSingleton<ICounterService, CounterService>();
            services.AddSingleton<IUserService, UserService>();

            services.AddSingleton<IBotMessageFormatter, BotMessageFormatter>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMetricsEndpoint();

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CounterAssistant.API v1"));

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapHealthChecks("/health/liveness", HealthCheck.DefaultOptions);
                endpoints.MapHealthChecks("/health/readiness", HealthCheck.DefaultOptions);

                endpoints.MapCodeCoverage();
            });
        }
    }
}
