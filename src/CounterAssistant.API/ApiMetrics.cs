using App.Metrics;
using App.Metrics.Counter;

namespace CounterAssistant.API
{
    public static class ApiMetrics
    {
        public static CounterOptions SucessfullyFinishedJobs => new CounterOptions 
        {
            Name = "background_job_successully_finished",
            MeasurementUnit = Unit.Items
        };

        public static CounterOptions FailedJobs => new CounterOptions 
        {
            Name = "background_job_failed",
            MeasurementUnit = Unit.Items
        };
    }
}
