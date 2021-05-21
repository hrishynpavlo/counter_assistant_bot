using CounterAssistant.Domain.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CounterAssistant.DataAccess.DTO
{
    public class UserDto
    {
        [BsonId]
        [BsonRepresentation(BsonType.Int32)]
        public int Id { get; set; }

        public long ChatId { get; set; }

        public string UserName { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public User ToDomain()
        {
            return new User
            {
                TelegramId = Id,
                TelegramChatId = ChatId,
                FirstName = FirstName,
                LastName = LastName,
                TelegramUserName = UserName
            }; 
        }

        public static UserDto FromDomain(User user)
        {
            return new UserDto
            {
                Id = user.TelegramId,
                ChatId = user.TelegramChatId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.TelegramUserName
            };
        }
    }
}
