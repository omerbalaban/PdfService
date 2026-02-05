using eLogo.PdfService.Services.Domain.Models;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services.Interfaces
{
    public interface IFailedConversionService
    {
        Task TrackFailedConversionAsync(FailedConversionModel failedConversion);
        void TrackFailedConversionFireAndForget(FailedConversionModel failedConversion);
    }
}
