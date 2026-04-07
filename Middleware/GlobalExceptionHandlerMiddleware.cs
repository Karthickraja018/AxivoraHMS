using System.Net;
using System.Text.Json;
using Axivora.Models;

namespace Axivora.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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

                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("The response has already started, the global exception handler will not write an error response.");
                    return;
                }

                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = HttpStatusCode.InternalServerError;
            var message = "An error occurred while processing your request.";

            switch (exception)
            {
                case KeyNotFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    message = exception.Message;
                    break;
                case ArgumentException:
                case InvalidOperationException:
                    statusCode = HttpStatusCode.BadRequest;
                    message = exception.Message;
                    break;
                // UnauthorizedAccessException represents a permissions (ownership) violation —
                // the user is authenticated but not authorised for this specific resource ? 403.
                // HTTP 401 is only produced by the JWT middleware for missing/invalid tokens.
                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Forbidden;
                    message = exception.Message;
                    break;
            }

            var response = new ErrorResponse
            {
                StatusCode = (int)statusCode,
                Message = message,
                Details = exception.Message
            };

            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
