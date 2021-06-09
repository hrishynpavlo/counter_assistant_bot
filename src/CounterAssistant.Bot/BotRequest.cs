using Telegram.Bot.Types;

namespace CounterAssistant.Bot
{
    public class BotRequest
    {
        public string FirstName { get; }
        public string LastName { get; }
        public string UserName { get; }
        public int UserId { get; }
        public long ChatId { get; }
        public string Text { get; }

        public BotRequest(User user, long chatId, string text)
        {
            UserId = user.Id;
            UserName = user.Username;
            FirstName = user.FirstName;
            LastName = user.LastName;
            ChatId = chatId;
            Text = text;
        }

        public static BotRequest FromMessage(Message message) => new BotRequest(message.From, message.Chat.Id, message.Text);
        public static BotRequest FromCallback(CallbackQuery callback) => new BotRequest(callback.From, callback.Message.Chat.Id, callback.Data);
    }
}
