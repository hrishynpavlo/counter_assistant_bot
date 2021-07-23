using CounterAssistant.DataAccess.DTO;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CounterAssistant.DataAccess
{
    public interface ICategoryService
    {
        Task<string[]> GetCategories();
        Task<string> GetCategoryBySeller(string seller);
        Task AddMatch(string category, string seller);
    }

    public class CategoryService : ICategoryService
    {
        private readonly IMongoCollection<FinancialCategoryDto> _db;

        public CategoryService(IMongoCollection<FinancialCategoryDto> db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public Task AddMatch(string category, string seller)
        {
            throw new NotImplementedException();
        }

        public Task<string[]> GetCategories()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetCategoryBySeller(string seller)
        {
            throw new NotImplementedException();
        }
    }
}
