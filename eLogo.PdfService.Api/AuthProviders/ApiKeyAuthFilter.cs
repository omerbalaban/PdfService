using eLogo.PdfService.Services.Domain.Collections.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using eLogo.PdfService.Settings;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace eLogo.PdfService.Api.AuthProviders
{
public class ApiKeyAuthFilter : IAuthorizationFilter
    {
        // x-api-key best practice: No realm or db dependency needed
        public ApiKeyAuthFilter() { }

        /// <summary>
        /// Basic authentication 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (string.IsNullOrWhiteSpace(Settings.Settings.AppSetting.ApiKey))
                return;

            string apiKeyHeader = context.HttpContext.Request.Headers["x-api-key"];
            string configuredApiKey = Settings.Settings.AppSetting.ApiKey;
            if (!string.IsNullOrWhiteSpace(apiKeyHeader) && !string.IsNullOrWhiteSpace(configuredApiKey) && apiKeyHeader == configuredApiKey)
            {
                return;
            }

            context.HttpContext.Response.StatusCode = 401;
            ReturnUnauthorizedResult(context);
        }

        /// <summary>
        /// ReturnUnauthorizedResult
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private void ReturnUnauthorizedResult(AuthorizationFilterContext context)
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
