namespace eLogo.PdfService.Models
{

    public class HtmlToPdfModel
    {

        public string CorrelationId { get; set; }

        public string Base64HtmlContent { get; set; }

        public string PageSize { get; set; }

        public string PageOrientation { get; set; }

        public int Margins { get; set; }

        public string OpenPassword { get; set; }

        public int? PdfConverter { get; set; }

        public AttachmentModel[] Attachments { get; set; }

        public PropertyItem[] CustomPropertyItems { get; set; }

        public string DocumentTitle { get; set; }

        public int? Zoom { get; set; }
                
    }
    

}