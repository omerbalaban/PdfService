using eLogo.PdfService.Services.Domain.Base;
using eLogo.PdfService.Services.Domain.Collections.Interfaces;
using eLogo.PdfService.Services.Domain.Models;
using MongoDB.Driver;
using System.Linq;

namespace eLogo.PdfService.Services.Domain.Collections
{
    public class ApiKeyCollection : GenericMongoCollection<ApiKeyModel>, IApiKeyCollection
    {

        const string DB_NAME = "PdfProvider"; 
        const string COLLECTION_NAME = "ApiKey";

        public ApiKeyCollection(IMongoClient connection)
            : base(connection, DB_NAME, COLLECTION_NAME) { }


        public ApiKeyCollection(IMongoClient connection, string dbName)
            : base(connection, string.IsNullOrEmpty(dbName) ? DB_NAME : dbName, COLLECTION_NAME)
        {
        }

        public ApiKeyModel GetBySecret(string clientSecret)
        {
            return this.Filter(c => c.ClientSecret == clientSecret).FirstOrDefault();
        }

         
    }
}
