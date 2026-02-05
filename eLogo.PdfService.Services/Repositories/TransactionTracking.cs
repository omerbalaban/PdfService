using MongoDB.Driver;

namespace eLogo.PdfService.Services.Repositories
{
    public class TransactionTracking : MongoRepository<Domain.Models.TransactionTrackingModel>, ITransactionTracking
    {
        public TransactionTracking(IMongoDatabase database) : base(database, "TransactionTracking")
        {
        }
    }
}
