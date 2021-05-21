using CounterAssistant.DataAccess;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace CounterAssistant.Bot
{
    public interface IContextProvider
    {
        Task<ChatContext> GetContext(int userId);
        Task<ChatContext> GetContext(Message message);
    }

    public class InMemoryContextProvider : IContextProvider
    {
        private readonly IUserStore _userStore;
        private readonly ConcurrentDictionary<int, ChatContext> _cache;

        public InMemoryContextProvider(IUserStore userStore)
        {
            _cache = new ConcurrentDictionary<int, ChatContext>();
            _userStore = userStore;
        }

        public async Task<ChatContext> GetContext(int userId)
        {
            return _cache.GetOrAdd(userId, new ChatContext { UserId = userId });
        }

        public async Task<ChatContext> GetContext(Message message)
        {
            var userId = message.From.Id;

            if (!_cache.TryGetValue(userId, out var context))
            {
                var user = await _userStore.GetUserAsync(userId);
                if (user == null)
                {
                    user = new Domain.Models.User
                    {
                        TelegramChatId = message.Chat.Id,
                        TelegramId = message.From.Id,
                        FirstName = message.From.FirstName,
                        LastName = message.From.LastName,
                        TelegramUserName = message.From.Username
                    };
                    await _userStore.CreateUserAsync(user);
                }

                context = MapToContext(message, user);
                _cache.AddOrUpdate(userId, context, (_, __) => context);
            }

            return context;
        }

        private ChatContext MapToContext(Message message, Domain.Models.User user)
        {
            return new ChatContext
            {
                ChatId = user.TelegramChatId,
                UserId = user.TelegramId,
                UserName = user.TelegramUserName,
                Name = $"{user.FirstName} {user.LastName}",
            };
        }
    }
}
