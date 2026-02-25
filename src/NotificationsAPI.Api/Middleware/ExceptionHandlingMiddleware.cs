using NotificationsAPI.Application.Common.Models;
using NotificationsAPI.Domain.Exceptions;

namespace NotificationsAPI.Api.Middleware;

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
            _logger.LogError(exception, "Ocorreu uma exceção não tratada: {Message}", exception.Message);
            context.Response.ContentType = "application/json";

            ErrorDetails errorDetails;

            switch (exception)
            {
                case ValidationException validationEx:
                    errorDetails = ErrorDetails.ErrorResponse(StatusCodes.Status400BadRequest, validationEx.Message, validationEx.Errors, context.TraceIdentifier);
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    break;
                case NotFoundException notFoundEx:
                    errorDetails = ErrorDetails.ErrorResponse(StatusCodes.Status404NotFound, notFoundEx.Message, null, context.TraceIdentifier);
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    break;
                case AutenticacaoException autenticacaoEx:
                    errorDetails = ErrorDetails.ErrorResponse(StatusCodes.Status401Unauthorized, autenticacaoEx.Message, null, context.TraceIdentifier);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    break;
                case UnauthorizedException unauthorizedEx:
                    errorDetails = ErrorDetails.ErrorResponse(StatusCodes.Status401Unauthorized, unauthorizedEx.Message, null, context.TraceIdentifier);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    break;
                case ForbiddenException forbiddenEx:
                    errorDetails = ErrorDetails.ErrorResponse(StatusCodes.Status403Forbidden, forbiddenEx.Message, null, context.TraceIdentifier);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    break;
                case DomainException domainEx:
                    errorDetails = ErrorDetails.ErrorResponse(StatusCodes.Status400BadRequest, domainEx.Message, null, context.TraceIdentifier);
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    break;
                default:
                    errorDetails = ErrorDetails.ErrorResponse(StatusCodes.Status500InternalServerError, "Ocorreu um erro interno no servidor", null, context.TraceIdentifier);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    break;
            }
            await context.Response.WriteAsJsonAsync(errorDetails);
        }
    }
}
