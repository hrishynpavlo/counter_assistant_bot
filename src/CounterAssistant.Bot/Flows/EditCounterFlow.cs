using CounterAssistant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CounterAssistant.Bot.Flows
{
    public class EditCounterFlow
    {
        private Counter _counter;

        public EditCounterFlow(Counter counter)
        {
            _counter = counter;
        }

        public Counter Counter => _counter;
    }

}
