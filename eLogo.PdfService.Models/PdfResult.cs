using System;

namespace eLogo.PdfService.Models
{

    public class PdfResult
    { 
        public bool Success { get; set; } 
        public string Message { get; set; } 
        public string Id { get; set; } 
        public string Content { get; set; }
        public byte[] AsByteArray()
        {
            return !string.IsNullOrEmpty(Content) ? Convert.FromBase64String(Content) : null;
        }
    }
}