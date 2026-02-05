using eLogo.PdfService.Services.Domain.Models;
using MongoDB.Driver;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services.Infrastructure
{
    public static class MongoDbIndexInitializer
    {
        public static async Task CreateIndexesAsync(IMongoDatabase database)
        {
            Trace.TraceInformation("MongoDB: Creating indexes...");

            try
            {
                await CreateApiKeyIndexesAsync(database);
                await CreatePdfTransactionIndexesAsync(database);
                await CreateFailedConversionsIndexesAsync(database);
                
                Trace.TraceInformation("MongoDB: ✓ All indexes created successfully");
            }
            catch (Exception ex)
            {
                Trace.TraceError($"MongoDB: ✗ Index creation failed: {ex.Message}");
                throw;
            }
        }

        private static async Task CreateApiKeyIndexesAsync(IMongoDatabase database)
        {
            var collection = database.GetCollection<ClientCredentialsModel>("ClientCredentials");

            // 1. Unique index on ApiKey field
            var apiKeyIndexModel = new CreateIndexModel<ClientCredentialsModel>(
                Builders<ClientCredentialsModel>.IndexKeys.Ascending(x => x.ApiKey),
                new CreateIndexOptions { Unique = true, Name = "idx_apiKey_unique" }
            );


            // 3. Compound index on ApiKey + Status for fast authentication
            var apiKeyStatusIndexModel = new CreateIndexModel<ClientCredentialsModel>(
                Builders<ClientCredentialsModel>.IndexKeys
                    .Ascending(x => x.ApiKey)
                    .Ascending(x => x.Status),
                new CreateIndexOptions { Name = "idx_apiKey_status" }
            );

            // 5. Index on Metadata.ExpiresAt for TTL cleanup (sparse index)
            var expiresAtIndexModel = new CreateIndexModel<ClientCredentialsModel>(
                Builders<ClientCredentialsModel>.IndexKeys.Ascending("Metadata.ExpiresAt"),
                new CreateIndexOptions { Name = "idx_expiresAt", Sparse = true }
            );

            // 6. Index on RateLimit.ResetAt for quota management
            var resetAtIndexModel = new CreateIndexModel<ClientCredentialsModel>(
                Builders<ClientCredentialsModel>.IndexKeys.Ascending("RateLimit.ResetAt"),
                new CreateIndexOptions { Name = "idx_rateLimit_resetAt", Sparse = true }
            );

            await collection.Indexes.CreateManyAsync(new[]
            {
                apiKeyIndexModel,
                apiKeyStatusIndexModel,
                expiresAtIndexModel,
                resetAtIndexModel
            });

            Trace.TraceInformation("  ✓ ApiKey indexes created");
        }

        private static async Task CreatePdfTransactionIndexesAsync(IMongoDatabase database)
        {
            var collection = database.GetCollection<TransactionTrackingModel>("TransactionTracking");

            // 1. Index on CorrelationId for lookup
            var correlationIdIndexModel = new CreateIndexModel<TransactionTrackingModel>(
                Builders<TransactionTrackingModel>.IndexKeys.Ascending(x => x.CorrelationId),
                new CreateIndexOptions { Name = "idx_correlationId" }
            );

            // 2. Index on CreatedAt for time-based queries and sorting
            var createdAtIndexModel = new CreateIndexModel<TransactionTrackingModel>(
                Builders<TransactionTrackingModel>.IndexKeys.Descending(x => x.CreatedAt),
                new CreateIndexOptions { Name = "idx_createdAt_desc" }
            );


            // 6. Index on IpAddress for client tracking
            var ipAddressIndexModel = new CreateIndexModel<TransactionTrackingModel>(
                Builders<TransactionTrackingModel>.IndexKeys.Ascending(x => x.IpAddress),
                new CreateIndexOptions { Name = "idx_ipAddress", Sparse = true }
            );

            // 8. TTL Index - automatically delete old transactions after 90 days
            var ttlIndexModel = new CreateIndexModel<TransactionTrackingModel>(
                Builders<TransactionTrackingModel>.IndexKeys.Ascending(x => x.CreatedAt),
                new CreateIndexOptions 
                { 
                    Name = "idx_createdAt_ttl",
                    ExpireAfter = TimeSpan.FromDays(90) // Auto-delete after 90 days
                }
            );

            await collection.Indexes.CreateManyAsync(new[]
            {
                correlationIdIndexModel,
                createdAtIndexModel,
                ipAddressIndexModel,
                ttlIndexModel
            });

            Trace.TraceInformation("  ✓ PdfApiTransaction indexes created");
        }

        private static async Task CreateFailedConversionsIndexesAsync(IMongoDatabase database)
        {
            var collection = database.GetCollection<FailedConversionModel>("FailedConversions");

            // 1. Index on CorrelationId for lookup
            var correlationIdIndexModel = new CreateIndexModel<FailedConversionModel>(
                Builders<FailedConversionModel>.IndexKeys.Ascending(x => x.CorrelationId),
                new CreateIndexOptions { Name = "idx_failedConv_correlationId" }
            );


            // 3. Index on IpAddress for tracking
            var ipAddressIndexModel = new CreateIndexModel<FailedConversionModel>(
                Builders<FailedConversionModel>.IndexKeys.Ascending(x => x.IpAddress),
                new CreateIndexOptions { Name = "idx_failedConv_ipAddress", Sparse = true }
            );

            // 4. TTL Index - automatically delete old failed conversions after 7 days
            var ttlIndexModel = new CreateIndexModel<FailedConversionModel>(
                Builders<FailedConversionModel>.IndexKeys.Ascending(x => x.CreatedAt),
                new CreateIndexOptions 
                { 
                    Name = "idx_failedConv_createdAt_ttl",
                    ExpireAfter = TimeSpan.FromDays(7) // Auto-delete after 7 days
                }
            );

            await collection.Indexes.CreateManyAsync(new[]
            {
                correlationIdIndexModel,
                ipAddressIndexModel,
                ttlIndexModel
            });

            Trace.TraceInformation("  ✓ FailedConversions indexes created");
        }
    }
}
