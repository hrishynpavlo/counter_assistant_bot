using CounterAssistant.Domain.Models;

namespace CounterAssistant.Domain.Builders
{
    public class CounterBuilder
    {
        private string _title;
        private ushort _step;

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
            return new Counter(_title, 0, _step, true);
        }

        public static CounterBuilder Default => new CounterBuilder();
    }
}
