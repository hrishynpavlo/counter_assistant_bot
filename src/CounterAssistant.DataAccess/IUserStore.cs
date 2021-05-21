using CounterAssistant.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CounterAssistant.DataAccess
{
    public interface IUserStore
    {
        Task<User> GetUserAsync(int id);
        Task CreateUserAsync(User user);
    }
}
