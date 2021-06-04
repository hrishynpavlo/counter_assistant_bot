using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Filters;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace CounterAssistant.API
{
    [ExcludeFromCodeCoverage]
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
                    config.SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false)
                        .AddJsonFile("secrets.json", optional: true)
                        .AddEnvironmentVariables(prefix: "CA_")
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
                        .Filter.ByExcluding(Matching.FromSource("Quartz"))
                        .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
