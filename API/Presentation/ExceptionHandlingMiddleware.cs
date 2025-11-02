using Amazon.S3;
using System.Net;
using System.Runtime.InteropServices;

namespace Presentation
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try { await _next(context); }
            catch (AmazonS3Exception ex) { context.Response.StatusCode = (int)ex.StatusCode; await context.Response.WriteAsync($"S3 Error: {ex.Message}"); }
            catch (ExternalException ex) { context.Response.StatusCode = (int)HttpStatusCode.FailedDependency; await context.Response.WriteAsync(ex.Message); }
            catch (ArgumentException ex) { context.Response.StatusCode = (int)HttpStatusCode.BadRequest; await context.Response.WriteAsJsonAsync(new { error = ex.Message }); }
            catch (IOException ex) { context.Response.StatusCode = (int)HttpStatusCode.BadRequest; await context.Response.WriteAsJsonAsync(new { error = ex.Message }); }
            catch (Exception ex) { context.Response.StatusCode = (int)HttpStatusCode.InternalServerError; await context.Response.WriteAsJsonAsync(new { error = ex.Message }); }
        }
    }
}
