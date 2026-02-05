using MongoDB.Driver;

namespace eLogo.PdfService.Services.Repositories
{
    public class FailedConversionRepository : MongoRepository<Domain.Models.FailedConversionModel>, IFailedConversionRepository
    {
        public FailedConversionRepository(IMongoDatabase database) : base(database, "FailedConversions")
        {
        }
    }
}
