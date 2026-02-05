using eLogo.PdfService.Services.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services.Repositories
{
    public interface ITransactionTracking : IRepository<Domain.Models.TransactionTrackingModel>
    {
        // Add any specific methods for PdfTransaction here if needed
    }
}
