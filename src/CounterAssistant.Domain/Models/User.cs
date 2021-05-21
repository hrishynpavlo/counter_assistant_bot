using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CounterAssistant.Domain.Models
{
    public class User
    {
        public int TelegramId { get; set; }
        public long TelegramChatId { get; set; }
        public string TelegramUserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
