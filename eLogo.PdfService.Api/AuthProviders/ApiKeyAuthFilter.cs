using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using eLogo.PdfService.Services.Repositories;
using System.Text.Json;
using System.Threading.Tasks;

namespace eLogo.PdfService.Api.AuthProviders
{
    public class ApiKeyAuthFilter : IAsyncAuthorizationFilter
    {
        private readonly IClientCredentialsRepository _apiKeyRepository;

        public ApiKeyAuthFilter(IClientCredentialsRepository apiKeyRepository)
        {
            _apiKeyRepository = apiKeyRepository;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            string apiKeyHeader = context.HttpContext.Request.Headers["x-api-key"];

            if (string.IsNullOrWhiteSpace(apiKeyHeader))
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new JsonResult(new { error = "API key is required", message = "Please provide x-api-key header" })
                {
                    StatusCode = 401
                };
                return;
            }

            if (!apiKeyHeader.Equals(Settings.Settings.AppSetting.ApiKey, System.StringComparison.OrdinalIgnoreCase))
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new JsonResult(new { error = "Invalid or expired API key", message = "The provided API key is invalid, expired, or rate limit exceeded" })
                {
                    StatusCode = 401
                };
                return;
            }

            /*
            //var apiKeyModel = await _apiKeyRepository.ValidateAndIncrementUsageAsync(apiKeyHeader);
            if (apiKeyModel == null)
            {
                context.HttpContext.Response.StatusCode = 401;
                context.Result = new JsonResult(new { error = "Invalid or expired API key", message = "The provided API key is invalid, expired, or rate limit exceeded" })
                {
                    StatusCode = 401
                };
                return;
            }
            

            // Store API key info in HttpContext for later use
            context.HttpContext.Items["ApiKeyModel"] = apiKeyModel;
            context.HttpContext.Items["ClientApplicationId"] = apiKeyModel.ClientApplicationId;
            */
        }
    }
}

