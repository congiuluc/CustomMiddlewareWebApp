using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CustomMiddlewareWebApp.Middlewares
{
    public class HtmlMinifierMiddleware
    {
        private readonly RequestDelegate _next;

        public HtmlMinifierMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            //Original Response 
            var originalResponse = context.Response.Body;
            //Reference to body stream 
            using (var stream = new MemoryStream())
            {
                context.Response.Body = stream;

                //Execute Next Middleware 
                await _next(context);

                //Reset stream to start ingpoint 
                stream.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(stream))
                {
                    //Current HTML Body 
                    string responseBody = await reader.ReadToEndAsync();
                    //IF Response OK (200) and Content text/HTML 
                    if (context.Response.StatusCode == 200 && context.Response.ContentType.ToLower().Contains("text/html"))
                    {
                        //Regular expression to remove white space between tags 
                        responseBody = Regex.Replace(responseBody, @">\s+<", "><", RegexOptions.Compiled);

                        //Prepare new stream for new response body 
                        using (var memoryStream = new MemoryStream())
                        {
                            var bytes = Encoding.UTF8.GetBytes(responseBody);
                            memoryStream.Write(bytes, 0, bytes.Length);
                            memoryStream.Seek(0, SeekOrigin.Begin);

                            //Copy new stream to original stream 
                            await memoryStream.CopyToAsync(originalResponse, bytes.Length);

                        }
                    }
                }
            }
        }
    }
    public static class BuilderExtensions
    {
        public static IApplicationBuilder UseHtmlMinifier(this IApplicationBuilder app)
        {
           return app.UseMiddleware<HtmlMinifierMiddleware>();
        }

    }

}
