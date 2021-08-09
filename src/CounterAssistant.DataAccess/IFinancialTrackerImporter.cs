using CounterAssistant.DataAccess.DTO;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CounterAssistant.DataAccess
{
    public interface IFinancialTrackerImporter
    {
        Task<long> ImportAsync(Stream importStream, int telegramUserId); 
    }

    public class SpendeeImporter : IFinancialTrackerImporter
    {
        private readonly IMongoDatabase _db;

        private static Dictionary<string, string> _categoryMapping = new Dictionary<string, string>
        {
            ["Еда и напитки"] = "Grocery",
            ["Дом"] = "House",
            ["Образование"] = "Studying",
            ["Путешествия"] = "Travelling",
            ["Развлечения"] = "Entertainment",
            ["Подарки"] = "Gifts",
            ["Покупки"] = "Clothes"
        };

        public SpendeeImporter(IMongoDatabase db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<long> ImportAsync(Stream importStream, int telegramUserId)
        {
            var db = _db.GetCollection<FinancialTransaction>($"financial-tracker-transactions-{telegramUserId}");
            var records = new List<FinancialTransaction>();

            var date = await db.Find(x => true).SortByDescending(x => x.Date).Limit(1).Project(x => x.Date).FirstOrDefaultAsync();

            using var reader = new StreamReader(importStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
            foreach (var record in csv.GetRecords<SpendeeRecord>().Where(x => x.Date > date))
            {
                records.Add(new FinancialTransaction
                {
                    Id = Guid.NewGuid(),
                    Amount = Math.Abs(record.Amount),
                    Category = _categoryMapping.TryGetValue(record.Category, out var category) ? category : record.Category,
                    Date = record.Date.ToUniversalTime(),
                    Title = record.Category,
                    Commnets = record.Note
                });
             }

            var result = await db.BulkWriteAsync(records.Select(x => new InsertOneModel<FinancialTransaction>(x)));

            return result.InsertedCount;
        }

        public class SpendeeRecord
        {
            [Index(0)]
            public DateTime Date { get; set; }

            [Index(3)]
            public string Category { get; set; }

            [Index(4)]
            public decimal Amount { get; set; }

            [Index(6)]
            public string Note { get; set; }
        }
    }
}
