﻿using System;
using System.Collections.Generic;

namespace CounterAssistant.Domain.Models
{
    public class User
    {
        public int TelegramId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public UserBotInfo BotInfo { get; set; } 
    }

    public class UserBotInfo
    {
        public long ChatId { get; set; }
        public string LastCommand { get; set; }
        public Guid? SelectedCounterId { get; set; }
        public string UserName { get; set; }
        public CreateCounterFlowInfo CreateCounterFlowInfo { get; set; }
    }

    public class CreateCounterFlowInfo
    {
        public string State { get; set; }
        public Dictionary<string, object> Args { get; set; }
    }
}
