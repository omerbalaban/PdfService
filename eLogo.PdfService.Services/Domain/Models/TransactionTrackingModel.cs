using eLogo.PdfService.Services.Domain.Models.Base;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace eLogo.PdfService.Services.Domain.Models
{
    public class TransactionTrackingModel : BaseDocumentModel
    {
  
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedAt { get; set; }
        public string ClientKey { get; set; }
        public string CorrelationId { get; set; }
        public int RequestSize { get; set; }
        public int PdfConverter { get; set; }
        public string PageOrientation { get; set; }
        public string DocumentTitle { get; set; }
        public string Vkn { get; set; }
        public string Source { get; set; }
        public string ApplicationName { get; set; }
        public string EndPoint { get; set; }
        public string IpAddress { get; set; }

        [BsonIgnoreIfNull]
        public string UserAccounRef { get; set; }
    }
}
