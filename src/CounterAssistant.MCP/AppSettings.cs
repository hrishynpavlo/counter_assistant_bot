namespace CounterAssistant.MCP;

public class AppSettings
{
    public MongoSettings Mongo { get; set; } = new();
}

public class MongoSettings
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
    public string CountersCollectionName { get; set; }
}