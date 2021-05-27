using CounterAssistant.Bot.Flows;
using CounterAssistant.Domain.Models;

namespace CounterAssistant.Bot
{
    public class ChatContext
    {
        public int UserId { get; set; }
        public long ChatId { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Command { get; private set; }
        public Counter SelectedCounter { get; set; }
        public CreateCounterFlow CreateCounterFlow { get; private set; }

        public void SetCurrentCommand(string command)
        {
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

        public static ChatContext FromUser(User user, Counter counter)
        {
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
