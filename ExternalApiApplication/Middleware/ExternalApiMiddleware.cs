using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ExternalApiApplication.Middleware
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ExternalApiMiddleware
    {
        private readonly RequestDelegate _next;

        public ExternalApiMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var path = httpContext.Request.Path.ToString();
            var Method = httpContext.Request.Method;
            var QueryParam = httpContext.Request.QueryString.ToString();
            var File = httpContext.Request.Form.Files[0];

            var Handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            HttpClient client = new HttpClient(Handler);
            HttpResponseMessage response = new HttpResponseMessage();
            string ExternalApi = string.Empty;

            if (Method == "GET")
            {
                if (path.Length > 0)
                {
                    ExternalApi = "https://localhost:7228" + path;
                    response = await client.GetAsync(ExternalApi);
                }
                if (QueryParam.Length > 0 && path.Length > 0)
                {
                    ExternalApi = "https://localhost:7228" + path + QueryParam;
                    response = await client.GetAsync(ExternalApi);
                }
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var res = JsonObject.Parse(responseBody);
                        await httpContext.Response.WriteAsJsonAsync(responseBody);
                        return;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                if(File.Length > 0)
                {
                    if(path.Length > 0)
                    {
                        ExternalApi = "https://localhost:7228" + path;
                        response = await client.GetAsync(ExternalApi);
                    }
                    if (QueryParam.Length > 0 && path.Length > 0)
                    {
                        ExternalApi = "https://localhost:7228" + path + QueryParam;
                        response = await client.GetAsync(ExternalApi);
                    }
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                            httpContext.Response.ContentType = contentType;
                            if (contentType != null)
                            {
                                using (var stream = await response.Content.ReadAsStreamAsync())
                                {
                                    await stream.CopyToAsync(httpContext.Response.Body);
                                }
                            }
                            return;
                        }
                        catch (Exception ex)
                        {

                            throw ex.InnerException;
                        }
                    }
                }
            } 
            if(Method == "POST")
            {
                if(path.Length > 0)
                {
                    ExternalApi = "https://localhost:7228" + path;
                    var body = string.Empty;
                    httpContext.Request.EnableBuffering();
                    using(var stream = new StreamReader(httpContext.Request.Body,System.Text.Encoding.UTF8,leaveOpen:true))
                    {
                        body = await stream.ReadToEndAsync();
                        httpContext.Request.Body.Position = 0;
                    }
                    var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                    response = await client.PostAsync(ExternalApi, content);
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var ResponseBody = await response.Content.ReadAsStringAsync();
                            var res = JsonObject.Parse(ResponseBody);
                            await httpContext.Response.WriteAsJsonAsync(res);
                            return;
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                }
                if(File.Length > 0  && path.Length > 0)
                {
                    if (httpContext.Request.HasFormContentType)
                    {

                    }
                }
            }
            return;
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ExternalApiMiddlewareExtensions
    {
        public static IApplicationBuilder UseExternalApiMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExternalApiMiddleware>();
        }
    }
}
