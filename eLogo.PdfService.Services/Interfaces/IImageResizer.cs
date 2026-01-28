namespace eLogo.PdfService.Services.Interfaces
{
    public interface IImageResizer
    {
        string CleanImages(string htmlWithBase64Image);
    }
}
