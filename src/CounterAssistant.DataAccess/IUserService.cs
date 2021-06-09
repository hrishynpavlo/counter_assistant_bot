using CounterAssistant.DataAccess.DTO;
using CounterAssistant.Domain.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CounterAssistant.DataAccess
{
    public interface IUserService
    {
        Task CreateAsync(User user);

        Task<User> GetUserByIdAsync(int id);
        Task<Dictionary<int, User>> GetUsersByIdsAsync(IEnumerable<int> ids);

        Task UpdateUserChatInfoAsync(int userId, UserBotInfo chatInfo);
    }

    public class UserService : IUserService
    {
        private readonly IAsyncRepository<UserDto> _repository;

        public UserService(IAsyncRepository<UserDto> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task CreateAsync(User user)
        {
            var dbUser = UserDto.FromDomain(user);
            await _repository.CreateOneAsync(dbUser);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            var filter = Builders<UserDto>.Filter.Eq(x => x.Id, id);
            var user = await _repository.FindOneAsync(filter);

            return user?.ToDomain();
        }

        public async Task<Dictionary<int, User>> GetUsersByIdsAsync(IEnumerable<int> ids)
        {
            var filter = Builders<UserDto>.Filter.In(x => x.Id, ids);
            var users = await _repository.FindManyAsync(filter);

            return users.Select(u => u.ToDomain()).ToDictionary(k => k.TelegramId);
        }

        public async Task UpdateUserChatInfoAsync(int userId, UserBotInfo chatInfo)
        {
            var filter = Builders<UserDto>.Filter.Eq(x => x.Id, userId);
            var update = Builders<UserDto>.Update
                .Set(x => x.BotInfo.CreateCounterFlowInfo, chatInfo.CreateCounterFlowInfo)
                .Set(x => x.BotInfo.LastCommand, chatInfo.LastCommand)
                .Set(x => x.BotInfo.SelectedCounterId, chatInfo.SelectedCounterId);

            await _repository.UpdateOneAsync(filter, update);
        }
    }
}
