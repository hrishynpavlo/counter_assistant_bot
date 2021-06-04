using System;

namespace CounterAssistant.Bot.Flows
{
    public class FlowException : Exception 
    {
        public string FlowName { get; }

        public FlowException(string flowName, string message) : base($"Error during performing flow {flowName}: {message}")
        {
            FlowName = flowName;
        }
    }
}
