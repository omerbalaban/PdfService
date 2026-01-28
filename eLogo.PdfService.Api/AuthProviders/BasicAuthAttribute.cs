using Microsoft.AspNetCore.Mvc;
using System;

namespace eLogo.PdfService.Api.AuthProviders
{
    /// <summary>
    /// BasicAuthAttribute
    /// </summary>
    /// <returns></returns>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class BasicAuthAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Basic authentication 
        /// </summary>
        /// <param name="realm"></param>
        /// <returns></returns>
        public BasicAuthAttribute(string realm = @"My Realm") : base(typeof(BasicAuthFilter))
        {
            Arguments = new object[] { realm };
        }
    }
}
