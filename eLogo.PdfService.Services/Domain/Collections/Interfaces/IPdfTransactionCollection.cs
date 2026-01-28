using eLogo.PdfService.Services.Domain.Models;

namespace eLogo.PdfService.Services.Domain.Collections.Interfaces
{
    public interface IPdfTransactionCollection : Base.IGenericMongoCollection<PdfApiTransaction>
    {
    }
}
