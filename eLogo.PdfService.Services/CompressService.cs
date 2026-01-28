using eLogo.PdfService.Services.Interfaces;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace eLogo.PdfService.Services
{
    public class CompressService : ICompressService
    {

        public byte[] Compress(byte[] data)
        {
            using (var msi = new MemoryStream(data))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }
                // Nullify large byte array to help GC
                data = null;
                return mso.ToArray();
            }
        }

        public byte[] Decompress(byte[] data)
        {
            using (var msi = new MemoryStream(data))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }
                // Nullify large byte array to help GC
                data = null;
                return mso.ToArray();
            }

        }

        private static void CopyTo(Stream src, Stream dest)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
            try
            {
                int cnt;
                while ((cnt = src.Read(buffer, 0, buffer.Length)) != 0)
                {
                    dest.Write(buffer, 0, cnt);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public byte[] UnzipData(byte[] data)
        {
            using (var msi = new MemoryStream(data))
            using (var mso = new MemoryStream())
            {
                using (var archive = SharpCompress.Archives.Zip.ZipArchive.Open(msi))
                {
                    foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                    {
                        using var entryStream = entry.OpenEntryStream();
                        entryStream.CopyTo(mso);
                    }
                }
                // Nullify large byte array to help GC
                data = null;
                return mso.ToArray();
            }
        }

        public byte[] ZipData(byte[] data)
        {
            using (var mStm = new MemoryStream(data))
            using (var mso = new MemoryStream())
            {
                using (var archive = SharpCompress.Archives.Zip.ZipArchive.Create())
                {
                    archive.AddEntry("outfile.pdf", mStm);
                    archive.SaveTo(mso);
                }

                return mso.ToArray();
            }
        }
    }
}
