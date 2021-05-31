using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace CounterAssistant.API.Extensions
{
    public static class CodeCoverageExtensions
    {
        private readonly static string Report = "No data";

        static CodeCoverageExtensions()
        {
            if (File.Exists("Summary.txt"))
            {
                Report = File.ReadAllText("Summary.txt");
            }
        }

        public static void MapCodeCoverage(this IEndpointRouteBuilder endpoint, string path = "/code-coverage")
        {
            endpoint.MapGet(path, async context => 
            {
                await context.Response.WriteAsync(Report);
            });
        }
    }
}
