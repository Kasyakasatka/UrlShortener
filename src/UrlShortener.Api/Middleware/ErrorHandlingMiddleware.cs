using Domain.Custom_Exceptions;
using System.Net;
using System.Text.Json;

namespace UrlShortener.Api.Middleware
{
    public class ErrorDetails
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string? TraceId { get; set; }
        public string? Type { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var statusCode = (int)HttpStatusCode.InternalServerError;

            var errorDetails = new ErrorDetails
            {
                StatusCode = statusCode,
                Message = "Произошла непредвиденная ошибка.",
                TraceId = context.TraceIdentifier,
                Type = "InternalServerError"
            };

            switch (exception)
            {
                case NotFoundException notFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    errorDetails.StatusCode = statusCode;
                    errorDetails.Message = notFoundException.Message;
                    errorDetails.Type = "NotFoundException";
                    break;
                case ValidationException validationException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    errorDetails.StatusCode = statusCode;
                    errorDetails.Message = validationException.Message;
                    errorDetails.Type = "ValidationException";
                    break;
                case DuplicateAliasException duplicateAliasException:
                    statusCode = (int)HttpStatusCode.Conflict;
                    errorDetails.StatusCode = statusCode;
                    errorDetails.Message = duplicateAliasException.Message;
                    errorDetails.Type = "DuplicateAliasException";
                    break;
                default:
                    if (context.RequestServices.GetService<Microsoft.Extensions.Hosting.IHostEnvironment>()?.IsProduction() == true)
                    {
                        errorDetails.Message = "Произошла непредвиденная ошибка сервера.";
                    }
                    break;
            }

            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsync(errorDetails.ToString());
        }
    }
}
