using Serilog;
using Serilog.Filters;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;

namespace CounterAssistant.API
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog((_, _, config) =>
            {
                config
                    .Filter.ByExcluding(Matching.FromSource("Microsoft"))
                    .Filter.ByExcluding(Matching.FromSource("Quartz"))
                    .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information);
            });
            var startup = new Startup();
            startup.ConfigureServices(builder.Services);
            var app = builder.Build();
            startup.Configure(app, app.Environment);
            
            app.Run();
        }
    }
}
