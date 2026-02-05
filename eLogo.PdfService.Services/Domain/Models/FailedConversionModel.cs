using eLogo.PdfService.Services.Domain.Models.Base;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace eLogo.PdfService.Services.Domain.Models
{
    public class FailedConversionModel : BaseDocumentModel
    {
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedAt { get; set; }

        public string CorrelationId { get; set; }

        public string IpAddress { get; set; }

        [BsonIgnoreIfNull]
        public string UserAccountRef { get; set; }

        public byte[] HtmlBuffer { get; set; }

        public string DocumentTitle { get; set; }

        public string PageOrientation { get; set; }

        public string PageSize { get; set; }

        public int Margins { get; set; }

        public int? Zoom { get; set; }

        public bool IsZipped { get; set; }

        public string ErrorMessage { get; set; }

        public string ExceptionType { get; set; }
        public string InnerExceptionMessage { get; set; }
        public string StackTrace { get; set; }

        public int ContentLength { get; set; }

        public int PdfConverter { get; set; }
    }
}
