using System.ComponentModel;
using CounterAssistant.DataAccess.DTO;
using CounterAssistant.MCP;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Newtonsoft.Json;


var appSettings = new ConfigurationBuilder()
    .AddJsonFile("secrets.json", optional: true)
    .AddEnvironmentVariables()
    .Build()
    .Get<AppSettings>();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddSingleton(appSettings!)
    .AddSingleton<IMongoCollection<CounterDto>>(_ =>
    {
        var pack = new ConventionPack { new CamelCaseElementNameConvention() };

        ConventionRegistry.Register(
            "CamelCaseConvention",
            pack,
            t => true);

        //important: map csuuid as uuid 
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        var client = new MongoClient(appSettings!.Mongo.ConnectionString);
        var mongo = client.GetDatabase(appSettings.Mongo.DatabaseName);
        return mongo.GetCollection<CounterDto>(appSettings.Mongo.CountersCollectionName);
    })
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
    })
    .WithToolsFromAssembly();
    
var app = builder.Build();

app.MapMcp();

app.Run("http://localhost:5000");

[McpServerToolType]
public static class CounterTool
{
    [McpServerTool, Description("Get a list of counters and back to the client.")]
    public static string GetCounters(IMongoCollection<CounterDto> db)
    {
        return JsonConvert.SerializeObject(db.Find(x => true).ToList());
    }

    [McpServerTool, Description("Get a list of counters for specified user and back to the client.")]
    public static string GetUserCounters(IMongoCollection<CounterDto> db, long id)
    {
        return JsonConvert.SerializeObject(db.Find(x => x.UserId == id).ToList());
    }
}