using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CounterAssistant.DataAccess
{
    public interface IAsyncRepository<T> where T: class
    {
        Task<T> FindOneAsync(FilterDefinition<T> filter);
        Task<IEnumerable<T>> FindManyAsync(FilterDefinition<T> filter);

        Task<bool> CreateOneAsync(T entity);

        Task<bool> UpdateOneAsync(FilterDefinition<T> filter, UpdateDefinition<T> update);
        Task<bool> UpdateManyAsync(IEnumerable<UpdateOneModel<T>> update);

        Task<bool> RemoveOneAsync(FilterDefinition<T> filter);
    }

    public class AsyncRepository<T> : IAsyncRepository<T> where T: class
    {
        protected readonly IMongoCollection<T> _db;
        protected readonly ILogger<T> _logger;

        public AsyncRepository(IMongoCollection<T> db, ILogger<T> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public virtual async Task<bool> CreateOneAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            try
            {
                await _db.InsertOneAsync(entity);
                return true;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "error during inserting entity {entity}", JsonConvert.SerializeObject(entity));
                return false;
            }
        }

        public virtual async Task<IEnumerable<T>> FindManyAsync(FilterDefinition<T> filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            return await _db.Find(filter).ToListAsync();
        }

        public virtual async Task<T> FindOneAsync(FilterDefinition<T> filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            return await _db.Find(filter).FirstOrDefaultAsync();
        }

        public virtual async Task<bool> RemoveOneAsync(FilterDefinition<T> filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            var result = await _db.DeleteOneAsync(filter);
            return result.IsAcknowledged;
        }

        public virtual async Task<bool> UpdateManyAsync(IEnumerable<UpdateOneModel<T>> update)
        {
            if (update == null) throw new ArgumentNullException(nameof(update));

            var result = await _db.BulkWriteAsync(update);
            return result.IsAcknowledged && result.ModifiedCount == result.MatchedCount;
        }

        public virtual async Task<bool> UpdateOneAsync(FilterDefinition<T> filter, UpdateDefinition<T> update)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (update == null) throw new ArgumentNullException(nameof(update));

            try
            {
                await _db.FindOneAndUpdateAsync(filter, update);
                return true;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "error during updating {operation}", update.ToString());
                return false;
            }
        }
    }
}
