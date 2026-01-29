using Microsoft.AspNetCore.Mvc;
using System;

namespace eLogo.PdfService.Api.AuthProviders
{
    /// <summary>
    /// ApiKeyAuthAttribute
    /// </summary>
    /// <returns></returns>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAuthAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// API Key authentication 
        /// </summary>
        public ApiKeyAuthAttribute() : base(typeof(ApiKeyAuthFilter))
        {
        }
    }
}
