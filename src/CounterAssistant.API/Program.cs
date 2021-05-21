using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Filters;

namespace CounterAssistant.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(config =>
                {
                    config.AddJsonFile("appSettings.json", optional: false)
                        .AddJsonFile("secrets.json", optional: true)
                        .AddEnvironmentVariables()
                        .Build();
                })
                .ConfigureLogging(config =>
                {
                    config.ClearProviders();
                    
                })
                .UseSerilog((_, __, config) => 
                {
                    config
                        .Filter.ByExcluding(Matching.FromSource("Microsoft"))
                        .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
