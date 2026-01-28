using eLogo.PdfService.Services.Domain.Base;
using eLogo.PdfService.Services.Domain.Models;

namespace eLogo.PdfService.Services.Domain.Collections.Interfaces
{
    public interface IApiKeyCollection : IGenericMongoCollection<ApiKeyModel>
    {
        ApiKeyModel GetApiKey(string client_id, string client_secret);
         

    }
}
