using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace eLogo.PdfService.Services.Domain.Models.Base
{
    public abstract class BaseDocumentModel
    {
        [BsonId]
        public ObjectId Id { get; set; }
    }
}
