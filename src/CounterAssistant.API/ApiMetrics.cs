using App.Metrics;
using App.Metrics.Counter;

namespace CounterAssistant.API
{
    public static class ApiMetrics
    {
        public static CounterOptions JobStarted => new CounterOptions 
        {
            Name = "background_job_started",
            MeasurementUnit = Unit.Items
        };
    }
}
