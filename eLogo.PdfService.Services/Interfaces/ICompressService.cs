namespace eLogo.PdfService.Services.Interfaces
{
    public interface ICompressService
    {
        byte[] Compress(byte[] data);

        byte[] Decompress(byte[] data);

        byte[] UnzipData(byte[] data);

        byte[] ZipData(byte[] data);
    }
}
