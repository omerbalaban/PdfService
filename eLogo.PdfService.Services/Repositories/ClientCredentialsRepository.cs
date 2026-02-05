using eLogo.PdfService.Services.Domain.Models;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services.Repositories
{
    public class ClientCredentialsRepository : MongoRepository<ClientCredentialsModel>, IClientCredentialsRepository
    {
        public ClientCredentialsRepository(IMongoDatabase database) : base(database, "ClientCredentials")
        {
        }

        public async Task<ClientCredentialsModel> GetByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
        {
            return await FindOneAsync(x => x.ApiKey == apiKey && x.Status == "active", cancellationToken);
        }

        public async Task<ClientCredentialsModel> ValidateAndIncrementUsageAsync(string apiKey, CancellationToken cancellationToken = default)
        {
            var apiKeyModel = await GetByApiKeyAsync(apiKey, cancellationToken);
            
            if (apiKeyModel == null)
                return null;

            // Check if expired
            if (apiKeyModel.Metadata?.ExpiresAt.HasValue == true && 
                apiKeyModel.Metadata.ExpiresAt.Value < DateTime.Now)
            {
                return null;
            }

            // Check rate limit
            if (apiKeyModel.RateLimit != null)
            {
                if (apiKeyModel.RateLimit.ResetAt < DateTime.Now)
                {
                    // Reset the quota
                    apiKeyModel.RateLimit.Used = 0;
                    apiKeyModel.RateLimit.ResetAt = DateTime.Now.AddMonths(1);
                }

                if (apiKeyModel.RateLimit.Used >= apiKeyModel.RateLimit.Quota)
                {
                    // Rate limit exceeded
                    return null;
                }

                // Increment usage
                var filter = Builders<ClientCredentialsModel>.Filter.Eq(x => x.Id, apiKeyModel.Id);
                var update = Builders<ClientCredentialsModel>.Update
                    .Inc(x => x.RateLimit.Used, 1)
                    .Inc(x => x.Metadata.TotalUsageCount, 1)
                    .Set(x => x.Metadata.LastUsedAt, DateTime.Now);

                await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            }
            else
            {
                // No rate limit, just update usage
                var filter = Builders<ClientCredentialsModel>.Filter.Eq(x => x.Id, apiKeyModel.Id);
                var update = Builders<ClientCredentialsModel>.Update
                    .Inc(x => x.Metadata.TotalUsageCount, 1)
                    .Set(x => x.Metadata.LastUsedAt, DateTime.Now);

                await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            }

            return apiKeyModel;
        }
    }
}

