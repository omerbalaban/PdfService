using eLogo.PdfService.Services.Domain.Base;
using eLogo.PdfService.Services.Domain.Models;

namespace eLogo.PdfService.Services.Domain.Collections.Interfaces
{
    public interface IApiKeyCollection : IGenericMongoCollection<ApiKeyModel>
    {
        ApiKeyModel GetBySecret(string clientSecret);
    }
}
