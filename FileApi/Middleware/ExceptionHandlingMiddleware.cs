using System.Net;
using System.Text.Json;

namespace FileApi.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex) 
            {
                await handleExceptionAsync(httpContext, ex);
            }     
        }

        private Task handleExceptionAsync(HttpContext httpContext, Exception ex)
        {
            httpContext.Response.ContentType = "application/json";
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
            var errorDetails = new ErrorDetails
            {
                ErrorType = "Failure",
                ErrorMessage = ex.Message,
            };

            switch(ex)
            {
                case BadHttpRequestException:
                    statusCode = HttpStatusCode.BadRequest;
                    errorDetails.ErrorType = "Bad Request";
                    break;
                case InvalidDataException:
                    statusCode = HttpStatusCode.BadRequest;
                    errorDetails.ErrorType = "Bad Request - Invalid Data";
                    break;
                case FileNotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    errorDetails.ErrorType = "Not Found";
                    break;
                default:
                    break;
            }

            string response = JsonSerializer.Serialize(errorDetails);
            httpContext.Response.StatusCode = (int)statusCode;

            return httpContext.Response.WriteAsync(response);
        }

        public class ErrorDetails
        {
            public string ErrorType { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
}
