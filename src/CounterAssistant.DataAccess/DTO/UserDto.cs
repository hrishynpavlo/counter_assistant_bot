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

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public UserBotInfo BotInfo { get; set; }

        public User ToDomain()
        {
            return new User
            {
                TelegramId = Id,
                FirstName = FirstName,
                LastName = LastName,
                BotInfo = BotInfo
            }; 
        }

        public static UserDto FromDomain(User user)
        {
            return new UserDto
            {
                Id = user.TelegramId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                BotInfo = user.BotInfo
            };
        }
    }
}
