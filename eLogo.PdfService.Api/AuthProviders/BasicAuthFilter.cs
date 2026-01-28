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
    public class BasicAuthFilter : IAuthorizationFilter
    {
        private readonly AppSettings _appSettings;
        private readonly string _realm;
        private readonly IApiKeyCollection _apiKeyCollection;

        /// <summary>
        /// Basic authentication 
        /// </summary>
        /// <param name="appSettings"></param>
        /// <param name="realm"></param>
        /// <param name="apiKeyCollection"></param>
        /// <returns></returns>
        public BasicAuthFilter(AppSettings appSettings, string realm, IApiKeyCollection apiKeyCollection)
        {

            _realm = realm;
            if (string.IsNullOrWhiteSpace(_realm))
            {
                throw new ArgumentNullException(nameof(realm), @"Please provide a non-empty realm value.");
            }
            _apiKeyCollection = apiKeyCollection;
            _appSettings = appSettings;
        }

        /// <summary>
        /// Basic authentication 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            try
            {
                if (!_appSettings.AuthenticationEnable)
                    return;

                string authHeader = context.HttpContext.Request.Headers["Authorization"];
                if (authHeader != null && authHeader.StartsWith("Basic"))
                {
                    AuthenticationHeaderValue authHeaderValue = AuthenticationHeaderValue.Parse(authHeader);
                    if (authHeaderValue.Scheme.Equals(AuthenticationSchemes.Basic.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        string[] credentials = Encoding.UTF8
                                            .GetString(Convert.FromBase64String(authHeaderValue.Parameter ?? string.Empty))
                                            .Split(':', 2);
                        if (credentials.Length == 2)
                        {
                            var apiKey = _apiKeyCollection.GetApiKey(credentials[0], credentials[1]);
                            if (apiKey != null)
                                return;

                            context.HttpContext.Response.StatusCode = 401;
                            ReturnUnauthorizedResult(context);
                            return;

                        }
                    }
                }
                else
                {
                    context.HttpContext.Response.StatusCode = 401;
                    ReturnUnauthorizedResult(context);
                    return;
                }
            }
            catch (FormatException)
            {
                ReturnUnauthorizedResult(context);
            }
        }

        /// <summary>
        /// ReturnUnauthorizedResult
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private void ReturnUnauthorizedResult(AuthorizationFilterContext context)
        {
            // Return 401 and a basic authentication challenge (causes browser to show login dialog)
            context.HttpContext.Response.Headers["WWW-Authenticate"] = $"Basic realm=\"{_realm}\"";
            context.Result = new UnauthorizedResult();
        }
    }
}
