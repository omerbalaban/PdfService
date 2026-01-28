using eLogo.PdfService.Services.Domain.Models.Base;

namespace eLogo.PdfService.Services.Domain.Models
{
    public class ApiKeyModel : BaseDocumentModel
    {
        /// <summary>
        /// client_id for Api
        /// </summary>
        public string ClientID { get; set; }

        /// <summary>
        /// client_secret for Api
        /// </summary>
        public string ClientSecret { get; set; }
 
        /// <summary>
        /// Description ApiKey
        /// </summary>
        public string Description { get; set; }
    }
}
