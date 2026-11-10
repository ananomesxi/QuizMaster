using Microsoft.AspNetCore.Mvc;

namespace QuizMasterWeb.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            catch (Exception exception)
            {
                _logger.LogError(exception, "An API request failed.");

                var (statusCode, title) = exception switch
                {
                    KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),

                    ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),

                    UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),

                    InvalidOperationException => (StatusCodes.Status409Conflict, "Request conflicts with the current state"), _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
                };
                var message = statusCode == StatusCodes.Status500InternalServerError ? "An unexpected error occurred while processing your request.": exception.Message;
                var response = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = message
                };

                context.Response.StatusCode = statusCode;

                await context.Response.WriteAsJsonAsync(
                    response,
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken: context.RequestAborted);
            }

        }
    }
}
