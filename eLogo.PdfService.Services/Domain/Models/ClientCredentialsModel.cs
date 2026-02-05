using eLogo.PdfService.Services.Domain.Models.Base;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace eLogo.PdfService.Services.Domain.Models
{
    public class ClientCredentialsModel : BaseDocumentModel
    {
        /// <summary>
        /// Client Application Identifier
        /// </summary>
        public string ClientApplicationId { get; set; }

        /// <summary>
        /// Client Application Name
        /// </summary>
        public string ClientApplicationName { get; set; }

        /// <summary>
        /// API Key for authentication (e.g., ks_test_4f7e2a9b1c8d5f3a6b0e92d7c41f8a5e)
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// API Key Status (active, inactive, suspended)
        /// </summary>
        public string Status { get; set; } = "active";

        /// <summary>
        /// Rate Limiting Configuration
        /// </summary>
        public RateLimit RateLimit { get; set; }

        /// <summary>
        /// API Key Metadata
        /// </summary>
        public ApiKeyMetadata Metadata { get; set; }
    }

    public class RateLimit
    {
        /// <summary>
        /// Maximum number of requests allowed
        /// </summary>
        public int Quota { get; set; }

        /// <summary>
        /// Number of requests used in current period
        /// </summary>
        public int Used { get; set; }

        /// <summary>
        /// When the rate limit quota resets
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime ResetAt { get; set; }
    }

    public class ApiKeyMetadata
    {
        /// <summary>
        /// When the API key was created
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Last time the API key was used
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// When the API key expires (null = never expires)
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Total number of times this API key has been used
        /// </summary>
        public long TotalUsageCount { get; set; }
    }
}

