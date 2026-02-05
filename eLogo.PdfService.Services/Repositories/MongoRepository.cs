using eLogo.PdfService.Services.Domain.Models.Base;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services.Repositories
{
    public class MongoRepository<T> : IRepository<T> where T : BaseDocumentModel
    {
        protected readonly IMongoCollection<T> _collection;

        public MongoRepository(IMongoDatabase database, string collectionName)
        {
            _collection = database.GetCollection<T>(collectionName);
        }

        public async Task<T> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (!ObjectId.TryParse(id, out var objectId))
                return null;

            var filter = Builders<T>.Filter.Eq(x => x.Id, objectId);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _collection.Find(_ => true).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _collection.Find(predicate).ToListAsync(cancellationToken);
        }

        public async Task<T> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _collection.Find(predicate).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<T> InsertAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
            return entity;
        }

        public async Task<IEnumerable<T>> InsertManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            await _collection.InsertManyAsync(entities, cancellationToken: cancellationToken);
            return entities;
        }

        public async Task<bool> UpdateAsync(string id, T entity, CancellationToken cancellationToken = default)
        {
            if (!ObjectId.TryParse(id, out var objectId))
                return false;

            var filter = Builders<T>.Filter.Eq(x => x.Id, objectId);
            var result = await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            if (!ObjectId.TryParse(id, out var objectId))
                return false;

            var filter = Builders<T>.Filter.Eq(x => x.Id, objectId);
            var result = await _collection.DeleteOneAsync(filter, cancellationToken);
            return result.DeletedCount > 0;
        }

        public async Task<long> CountAsync(Expression<Func<T, bool>> predicate = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
                return await _collection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken);

            return await _collection.CountDocumentsAsync(predicate, cancellationToken: cancellationToken);
        }
    }
}
