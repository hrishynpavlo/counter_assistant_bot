using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CounterAssistant.API.HealthChecks
{
    public static class HealthCheck
    {
        private static JsonSerializerOptions SerializerOptions = new JsonSerializerOptions 
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, 
            WriteIndented = true
        };

        public static HealthCheckOptions DefaultOptions => new HealthCheckOptions 
        {
            ResultStatusCodes = new Dictionary<HealthStatus, int>
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable
            },
            AllowCachingResponses = false,
            ResponseWriter = JsonWritter
        };

        private static async Task JsonWritter(HttpContext context, HealthReport health)
        {
            var healthCheck = new AppHealthCheck
            {
                Healthy = health.Status == HealthStatus.Healthy,
                Checks = health.Entries.Select(x => new Check 
                {
                    Name = x.Key,
                    Health = x.Value.Status == HealthStatus.Healthy,
                    Tags = x.Value.Tags,
                    Error = x.Value.Exception?.Message
                }).ToArray()
            };

            await context.Response.WriteAsJsonAsync(healthCheck, options: SerializerOptions);
        }
    }

    public class AppHealthCheck
    {
        public bool Healthy { get; set; }
        public Check[] Checks { get; set; }
    }

    public class Check
    {
        public string Name { get; set; }
        public bool Health { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public string Error { get; set; }
    }
}
