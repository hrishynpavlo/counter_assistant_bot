using CounterAssistant.Bot.Flows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CounterAssistant.Bot
{
    public class ChatContext
    {
        public int UserId { get; set; }
        public long ChatId { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Command { get; set; }
        public CreateCounterFlow CreateCounterFlow { get; set; }
        public EditCounterFlow EditCounterFlow { get; set; }

        public void SetCurrentCommand(string command)
        {
            Command = command;
        }
    }
}
