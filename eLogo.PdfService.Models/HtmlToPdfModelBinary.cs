using MessagePack;

namespace eLogo.PdfService.Models
{
    [MessagePackObject]
    public class HtmlToPdfModelBinary
    {
        [Key(0)]
        public string CorrelationId { get; set; }
        [Key(1)]
        public byte[] Content { get; set; }
        [Key(2)]
        public string PageSize { get; set; }
        [Key(3)]
        public string PageOrientation { get; set; }
        [Key(4)]
        public int Margins { get; set; }
        [Key(5)]
        public string OpenPassword { get; set; }
        [Key(6)]
        public int? PdfConverter { get; set; }
        [Key(7)]
        public AttachmentModelBinary[] Attachments { get; set; }
        [Key(8)]
        public PropertyItemBinary[] CustomPropertyItems { get; set; }
        [Key(9)] 
        public string DocumentTitle { get; set; }
        [Key(10)]
        public int? Zoom { get; set; }

        [Key(11)]
        public bool IsZipped { get; set; } = false;

    }
}