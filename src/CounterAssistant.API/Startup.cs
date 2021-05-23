using CounterAssistant.API.HostedServices;
using CounterAssistant.API.Jobs;
using CounterAssistant.Bot;
using CounterAssistant.DataAccess;
using CounterAssistant.DataAccess.DTO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;

namespace CounterAssistant.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var appSettings = AppSettings.FromConfig(Configuration);

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "CounterAssistant.API", Version = "v1" });
            });

            services.AddSingleton<IMongoDatabase>(_ => 
            {
                var pack = new ConventionPack();
                pack.Add(new CamelCaseElementNameConvention());

                ConventionRegistry.Register(
                   "CamelCaseConvention",
                   pack,
                   t => true);

                var client = new MongoClient(appSettings.MongoHost);
                return client.GetDatabase(appSettings.MongoDatabase);
            });
            services.AddSingleton<IMongoCollection<UserDto>>(provider => 
            {
                var mongo = provider.GetService<IMongoDatabase>();
                return mongo.GetCollection<UserDto>(appSettings.MongoUserCollection);
            });
            services.AddSingleton<IMongoCollection<CounterDto>>(provider =>
            {
                var mongo = provider.GetService<IMongoDatabase>();
                return mongo.GetCollection<CounterDto>(appSettings.MongoCounterCollection);
            });

            services.AddSingleton<IUserStore, UserStore>();
            services.AddSingleton<ICounterStore, CounterStore>();

            services.AddSingleton<IContextProvider, InMemoryContextProvider>();

            services.AddSingleton<TelegramBotClient>(new TelegramBotClient(appSettings.TelegramBotAccessToken));
            services.AddSingleton<BotService>();

            services.AddHostedService<BotHostedService>();
            //services.AddHostedService<CounterProcessorHostedService>();

            services.AddQuartz(options => 
            {
                options.UseMicrosoftDependencyInjectionJobFactory();

                var jobKey = new JobKey("daily_counter_processing");

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
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CounterAssistant.API v1"));

            app.UseHttpsRedirection();

            app.UseRouting();

            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
