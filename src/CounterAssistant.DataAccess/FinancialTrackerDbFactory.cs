using CounterAssistant.DataAccess.DTO;
using MongoDB.Driver;
using System;

namespace CounterAssistant.DataAccess
{
    public class FinancialTrackerDbFactory
    {
        private const string PATTERN = "financial-tracker-transactions-{0}";

        private readonly IMongoDatabase _db;
        
        public FinancialTrackerDbFactory(IMongoDatabase db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public IMongoCollection<FinancialTransaction> Create(int telegramUserId)
        {
            var name = string.Format(PATTERN, telegramUserId);
            return _db.GetCollection<FinancialTransaction>(name);
        }
    }
}
