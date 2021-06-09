using CounterAssistant.Domain.Models;
using System.Collections.Generic;

namespace CounterAssistant.Domain.Builders
{
    public class CounterBuilder
    {
        private string _title;
        private ushort? _step;
        private bool? _isManual;

        public const string TitleArgKey = "name";
        public const string StepArgKey = "step";
        public const string IsManualArgKey = "isManual";
        public const ushort DefultStep = 1;
        public const bool DefaultIsManual = false;

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

        public CounterBuilder WithType(bool isManual)
        {
            _isManual = isManual;
            return this;
        }

        public Counter Build()
        {
            try
            {
                var counter = new Counter(_title, 0, _step ?? DefultStep, _isManual ?? DefaultIsManual);
                Reset();
                return counter;
            }
            catch
            {
                throw;
            }
        }

        public Dictionary<string, object> GetArgs()
        {
            var args = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(_title)) args[TitleArgKey] = _title;
            if (_step.HasValue) args[StepArgKey] = _step.Value;
            if (_isManual.HasValue) args[IsManualArgKey] = _isManual.Value;

            return args;
        }

        public static CounterBuilder Default => new CounterBuilder();

        private void Reset()
        {
            _title = string.Empty;
            _step = null;
            _isManual = null;
        }
    }
}
