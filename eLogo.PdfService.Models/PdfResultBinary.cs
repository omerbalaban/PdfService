using MessagePack;
using System;

namespace eLogo.PdfService.Models
{
    [MessagePackObject]
    public class PdfResultBinary
    {
        [Key(0)]
        public bool Success { get; set; }
        [Key(1)]
        public string Message { get; set; }
        [Key(2)]
        public string Id { get; set; }
        [Key(3)]
        public byte[] Content { get; set; }
        public string AsBase64String()
        {
            return (Content != null && Content.Length > 0) ? Convert.ToBase64String(Content) : null;
        }
    }
}
