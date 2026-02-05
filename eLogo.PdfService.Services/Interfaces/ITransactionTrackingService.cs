using eLogo.PdfService.Models;
using eLogo.PdfService.Services.Domain.Models;
using System.Reflection;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services.Interfaces
{
    public interface ITransactionTrackingService
    {
        Task TrackRequestAsync(TransactionTrackingModel transaction);
        void TrackRequestFireAndForget(TransactionTrackingModel transaction);
    }
}
