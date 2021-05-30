using CounterAssistant.Bot.Flows;
using CounterAssistant.Domain.Models;
using System;

namespace CounterAssistant.Bot
{
    public class ChatContext
    {
        public int UserId { get; set; }
        public long ChatId { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Command { get; private set; }
        public Counter SelectedCounter { get; private set; }
        public CreateCounterFlow CreateCounterFlow { get; private set; }

        public void SetCurrentCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) throw new ArgumentNullException(nameof(command));

            Command = command;
        }

        public void StartCreateCounterFlow()
        {
            CreateCounterFlow = new CreateCounterFlow();
        }

        public void FinishCreateCounterFlow()
        {
            CreateCounterFlow = null;
        }

        public void SelectCounter(Counter counter)
        {
            if (counter == null) throw new ArgumentNullException(nameof(counter));

            SelectedCounter = counter;
        }

        public void ClearSelectedCounter()
        {
            SelectedCounter = null;
        }

        public static ChatContext Restore(User user, Counter counter)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (user.BotInfo == null) throw new ArgumentNullException(nameof(user.BotInfo));

            return new ChatContext
            {
                ChatId = user.BotInfo.ChatId,
                Command = user.BotInfo.LastCommand,
                CreateCounterFlow = CreateCounterFlow.RestoreFromContext(user),
                Name = $"{user.FirstName} {user.LastName}",
                UserId = user.TelegramId,
                UserName = user.BotInfo.UserName,
                SelectedCounter = counter
            };
        }
    }
}
