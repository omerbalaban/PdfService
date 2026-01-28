using eLogo.PdfService.Services.Domain.Models.Base;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services.Domain.Base
{
    public abstract class GenericMongoCollection<TModel> : IGenericMongoCollection<TModel>
           where TModel : BaseDocumentModel
    {
        protected readonly IMongoCollection<TModel> mongoCollection;

        protected readonly IMongoClient _mongoClient;
        protected readonly IMongoDatabase mongoDatabase;

        protected GenericMongoCollection(IMongoClient mongoClient, string dbName, string collectionName)
        {
            _mongoClient = mongoClient;
            mongoDatabase = _mongoClient.GetDatabase(dbName);
            mongoCollection = mongoDatabase.GetCollection<TModel>(collectionName,null);
        }

        public virtual List<TModel> GetList()
        {
            return mongoCollection.Find(x => true).ToList();
        }

        public virtual TModel GetById(string id)
        {
            ObjectId docId = new ObjectId(id);
            return mongoCollection.Find<TModel>(m => m.Id == docId).FirstOrDefault();
        }

        public virtual TModel Create(TModel model)
        {
            var existingItem = mongoCollection.Find<TModel>(m => m.Id == model.Id).FirstOrDefault();

            if (existingItem == null)
                mongoCollection.InsertOne(model);
            else
                mongoCollection.ReplaceOne(m => m.Id == existingItem.Id, model);

            return model;
        }

        public virtual TModel Update(string id, TModel model)
        {
            var docId = new ObjectId(id);
            mongoCollection.ReplaceOne(m => m.Id == docId, model);
            return model;
        }

        public virtual void Update(ObjectId id, TModel model)
        {
            mongoCollection.ReplaceOne(m => m.Id == id, model);
        }

        public virtual void Delete(TModel model)
        {
            mongoCollection.DeleteOne(m => m.Id == model.Id);
        }

        public virtual void Delete(string id)
        {
            var docId = new ObjectId(id);
            mongoCollection.DeleteOne(m => m.Id == docId);
        }

        public IEnumerable<TModel> Filter(Expression<Func<TModel, bool>> filter)
        {
            return mongoCollection.Find(filter).ToList();
        }

        public TModel GetFirstOrDefault(Expression<Func<TModel, bool>> filter)
        {
            return mongoCollection.Find(filter).FirstOrDefault();
        }

        public TModel GetSingleOrDefault(Expression<Func<TModel, bool>> filter)
        {
            return mongoCollection.Find(filter).SingleOrDefault();
        }

        public long Count(Expression<Func<TModel, bool>> filter)
        {
            return mongoCollection.CountDocuments(filter);
        }

        public string GetId(Expression<Func<TModel, bool>> filter)
        {
            return mongoCollection.Find(filter).FirstOrDefault()?.Id.ToString()?.ToString();
        }

        public ObjectId GetBsonId(Expression<Func<TModel, bool>> filter)
        {
            return mongoCollection.Find(filter).FirstOrDefault().Id;
        }

        public virtual void FindOneAndReplace(Expression<Func<TModel, bool>> predicate, TModel replacement, CancellationToken cancellationToken = default)
        {
            mongoCollection.FindOneAndReplace(predicate, replacement);
        }

        public virtual async Task FindOneAndReplaceAsync(Expression<Func<TModel, bool>> predicate, TModel replacement, CancellationToken cancellationToken = default)
        {
            await mongoCollection.FindOneAndReplaceAsync(predicate, replacement, cancellationToken: cancellationToken);
        }

        public virtual async Task<TModel> InsertAsync(TModel model, CancellationToken cancellationToken = default)
        {
            await mongoCollection.InsertOneAsync(model, null, cancellationToken);
            return model;
        }

        public virtual async Task<List<TModel>> InsertManyAsync(List<TModel> models, CancellationToken cancellationToken = default)
        {
            await mongoCollection.InsertManyAsync(models, cancellationToken: cancellationToken);
            return models;
        }

        public async Task<List<TModel>> GetListAsync(Expression<Func<TModel, bool>> predicate = null, CancellationToken cancellationToken = default)
        {
            IAsyncCursor<TModel> entities;
            if (predicate != null)
                entities = await mongoCollection.FindAsync(predicate, cancellationToken: cancellationToken);
            else
                entities = await mongoCollection.FindAsync(_ => true, cancellationToken: cancellationToken);
            return await entities.ToListAsync(cancellationToken);
        }

        public async Task<TModel> GetFirstOrDefaultAsync(Expression<Func<TModel, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var entity = await mongoCollection.FindAsync(predicate, cancellationToken: cancellationToken);
            return await entity.FirstOrDefaultAsync(cancellationToken);
        }

        public virtual long GetDocumentCount()
        {
            long count = mongoCollection.CountDocuments(new BsonDocument());
            return count;
        }

        public virtual List<TModel> InsertMany(IClientSessionHandle session, List<TModel> models)
        {
            mongoCollection.InsertMany(session, models);
            return models;
        }
    }
}


