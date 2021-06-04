using CounterAssistant.Domain.Models;
using System.Collections.Generic;

namespace CounterAssistant.Domain.Builders
{
    public class CounterBuilder
    {
        private string _title;
        private ushort? _step;

        public const string TitleArgKey = "name";
        public const string StepArgKey = "step";
        public const ushort DefultStep = 1;

        public CounterBuilder WithName(string title)
        {
            _title = title;
            return this;
        }

        public CounterBuilder WithStep(ushort step)
        {
            _step = step;
            return this;
        }

        public Counter Build()
        {
            return new Counter(_title, 0, _step ?? DefultStep, true);
        }

        public Dictionary<string, object> GetArgs()
        {
            var args = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(_title)) args[TitleArgKey] = _title;
            if (_step.HasValue) args[StepArgKey] = _step.Value;

            return args;
        }

        public static CounterBuilder Default => new CounterBuilder();
    }
}
