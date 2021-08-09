using CounterAssistant.DataAccess;
using CounterAssistant.DataAccess.DTO;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CounterAssistant.API
{
    public class MonobankRecieverService : IHostedService, IDisposable
    {
        private const string DEFAULT_CATEGORY = "Other";

        private readonly IObservable<MonobankTransaction> _stream;
        private readonly FinancialTrackerDbFactory _dbFactory;
        private readonly IMongoCollection<UserDto> _userStore;
        private readonly IMongoCollection<FinancialCategoryDto> _categoryStore;

        private IDisposable _subscription;

        public MonobankRecieverService(FinancialTrackerDbFactory dbFactory, IMongoCollection<UserDto> userStore, IMongoCollection<FinancialCategoryDto> categoryStore, IPipeline<MonobankTransaction> pipeline)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _categoryStore = categoryStore ?? throw new ArgumentNullException(nameof(categoryStore));

            _stream = pipeline?.GetStream() ?? throw new ArgumentNullException(nameof(pipeline));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _subscription = _stream.Subscribe(async monobankTransaction => await OnMessage(monobankTransaction));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Dispose();
            return Task.CompletedTask;
        }

        private async Task OnMessage(MonobankTransaction monobankTransaction)
        {
            var user = await _userStore.Find(x => x.MonobankAccounts.Contains(monobankTransaction.Data.Account)).Limit(1).FirstOrDefaultAsync();
            if (user == null) return;

            var transaction = Map(monobankTransaction);
            await Match(transaction);

            var db = _dbFactory.Create(user.Id);
            await db.InsertOneAsync(transaction);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }

        private FinancialTransaction Map(MonobankTransaction transaction)
        {
            return new FinancialTransaction
            {
                Id = Guid.NewGuid(),
                Amount = (decimal)Math.Abs(transaction.Data.StatementItem.Amount) / 100,
                Date = DateTime.UnixEpoch.AddSeconds(transaction.Data.StatementItem.Time),
                Title = transaction.Data.StatementItem.Description,
                Comments = transaction.Data.StatementItem.Comment
            };
        }

        private async Task Match(FinancialTransaction transaction)
        {
            var category = await _categoryStore.Find(x => x.Sellers.Contains(transaction.Title)).Project(x => x.Name).FirstOrDefaultAsync() ?? DEFAULT_CATEGORY;
            transaction.Category = category;
        }
    }
}
