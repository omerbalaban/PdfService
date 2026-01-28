using eLogo.PdfService.Services.Domain.Collections.Interfaces;
using eLogo.PdfService.Services.Domain.Models;
using MongoDB.Driver;

namespace eLogo.PdfService.Services.Domain.Collections
{
    public class PdfTransactionCollection : Base.GenericMongoCollection<PdfApiTransaction>, IPdfTransactionCollection
    {
        const string DB_NAME = "PdfProvider";
        const string COLLECTION_NAME = "PdfApiTransaction";

        public PdfTransactionCollection(IMongoClient connection)
            : base(connection, DB_NAME, COLLECTION_NAME) { }


        public PdfTransactionCollection(IMongoClient connection, string dbName)
            : base(connection, string.IsNullOrEmpty(dbName) ? DB_NAME : dbName, COLLECTION_NAME)
        {
        }
    }
}
