using MessagePack;

namespace eLogo.PdfService.Models
{
    [MessagePackObject]
    public class PropertyItemBinary
    {
        [Key(0)]
        public string Key { get; set; }
        [Key(1)]
        public string Value { get; set; }
    }
}
