using System;
using Serilog;
using Serilog.Filters;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CounterAssistant.API.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace CounterAssistant.API
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static ILogger _logger;
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Filter.ByExcluding(Matching.FromSource("Microsoft"))
                .Filter.ByExcluding(Matching.FromSource("Quartz"))
                .Enrich.WithProperty("appVersion", AppSettings.AppVersion)
                .Enrich.WithProperty("appEnvironment", AppSettings.Environment)
                .Enrich.WithProperty("appName", AppSettings.AppName)
                .Enrich.FromLogContext()
                .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information, 
                    outputTemplate: "[{Timestamp:yyyy:MM:dd HH:mm:ss zzz}] [{SourceContext}] {NewLine}[{Level:u3}] {Message:j}{NewLine}{Exception}")
                .CreateLogger();
            
            _logger = Log.Logger.ForContext(typeof(Program));

            try
            {
                StartApp(args);
                _logger.Information("[Counter Assistant Bot] Successfully stopped.");
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "[Counter Assistant Bot] Crashed on startup.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void StartApp(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("secrets.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
            
            var appSettings = AppSettings.FromConfig(configuration);
            
            builder.Services.AddConfiguration(configuration, appSettings);
            
            builder.Services
                .AddMongoDbPersistance()
                .AddInMemoryCache();
            
            builder.Services
                .AddTelegramBot()
                .AddTelegramChatContextProvider();

            builder.Services
                .AddMetrics(appSettings)
                .AddHealthChecks(appSettings);

            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Counter Assistant Bot API", Version = "v1" });
            });
            
            builder.Host.UseSerilog();
            
            var app = builder.Build();
            
            app.UseMetricsEndpoint();

            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CounterAssistant.API v1"));

            app.UseRouting();

            app.MapControllers();
            app.MapGet("/", context =>
            {
                context.Response.Redirect("/swagger");
                return Task.CompletedTask;
            });
            app.MapHealthChecks("/health/liveness", HealthCheck.DefaultOptions);
            app.MapHealthChecks("/health/readiness", HealthCheck.DefaultOptions);
            
            _logger.Information("[Counter Assistant Bot] Starting.");
            
            app.Run();
        }
    }
}
