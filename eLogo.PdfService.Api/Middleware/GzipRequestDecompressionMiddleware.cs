using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace eLogo.PdfService.Api.Middleware
{
    /// <summary>
    /// Middleware to handle gzip-compressed incoming requests
    /// </summary>
    public class GzipRequestDecompressionMiddleware
    {
        private readonly RequestDelegate _next;

        public GzipRequestDecompressionMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var contentEncoding = context.Request.Headers["Content-Encoding"].ToString().ToLowerInvariant();

            if (contentEncoding.Contains("gzip"))
            {
                var originalBody = context.Request.Body;

                try
                {
                    using (var decompressionStream = new GZipStream(originalBody, CompressionMode.Decompress, leaveOpen: true))
                    {
                        var decompressedStream = new MemoryStream();
                        await decompressionStream.CopyToAsync(decompressedStream);
                        decompressedStream.Seek(0, SeekOrigin.Begin);

                        context.Request.Body = decompressedStream;
                        context.Request.Headers.Remove("Content-Encoding");

                        if (context.Request.Headers.ContainsKey("Content-Length"))
                        {
                            context.Request.Headers["Content-Length"] = decompressedStream.Length.ToString();
                        }

                        await _next(context);
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync($"Failed to decompress gzip request: {ex.Message}");
                    return;
                }
                finally
                {
                    context.Request.Body = originalBody;
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}
