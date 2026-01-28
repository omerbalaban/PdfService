using eLogo.PdfService.Models;
using System.Threading.Tasks;

namespace eLogo.PdfService.Services.Interfaces
{
    public interface IPdfConvertService
    {
        Task<PdfResultBinary> ConvertHtmlToPdf(HtmlToPdfModelBinary model);

        Task<PdfResultBinary[]> ConvertHtmlToImage(HtmlToPdfModelBinary model);
    }
}
