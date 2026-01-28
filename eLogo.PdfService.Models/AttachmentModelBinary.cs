using MessagePack;

namespace eLogo.PdfService.Models
{
    [MessagePackObject]
    public class AttachmentModelBinary
    {
        [Key(0)]
        public string FileName { get; set; }
        [Key(1)]
        public byte[] Data { get; set; }
    }
}
