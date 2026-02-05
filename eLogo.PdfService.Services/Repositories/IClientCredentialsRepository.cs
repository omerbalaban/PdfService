using eLogo.PdfService.Services.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services.Repositories
{
    public interface IClientCredentialsRepository : IRepository<ClientCredentialsModel>
    {
        Task<ClientCredentialsModel> GetByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
        Task<ClientCredentialsModel> ValidateAndIncrementUsageAsync(string apiKey, CancellationToken cancellationToken = default);
    }
}

