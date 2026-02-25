using NotificationsAPI.Application.Common.Models;

namespace NotificationsAPI.Api.Helpers;

public static class ApiResponses
{
    public static IResult Ok<T>(T data, string message = "Operação realizada com sucesso")
    {
        var response = ApiResponse<T>.SuccessResponse(data, message);
        return Results.Ok(response);
    }

    public static IResult OkMessage(string message = "Operação realizada com sucesso")
    {
        var response = ApiResponse<object>.SuccessResponse(null, message);
        return Results.Ok(response);
    }

    public static IResult Created<T>(string uri, T data, string message = "Operação realizada com sucesso")
    {
        var response = ApiResponse<T>.SuccessResponse(data, message);
        return Results.Created(uri, response);
    }

    public static IResult BadRequest(string key, string message)
    {
        var errors = new Dictionary<string, string[]> { { key, new[] { message } } };
        var errorDetails = ErrorDetails.ErrorResponse(StatusCodes.Status400BadRequest, message, errors);
        return Results.BadRequest(errorDetails);
    }

    public static IResult BadRequestMultiple(string message, Dictionary<string, string[]> errors)
    {
        var errorDetails = ErrorDetails.ErrorResponse(StatusCodes.Status400BadRequest, message, errors);
        return Results.BadRequest(errorDetails);
    }

    public static IResult NotFound(string key, string message, Dictionary<string, string[]>? errors = null)
    {
        var finalErrors = errors ?? new Dictionary<string, string[]> { { key, new[] { message } } };

        var errorDetails = ErrorDetails.ErrorResponse(StatusCodes.Status404NotFound, message, finalErrors);
        return Results.NotFound(errorDetails);
    }

    public static IResult Unauthorized(string message)
    {
        var errorDetails = ErrorDetails.ErrorResponse(StatusCodes.Status401Unauthorized, message);
        return Results.Json(errorDetails, statusCode: StatusCodes.Status401Unauthorized);
    }

    public static IResult Problem(string message = "Ocorreu um erro inesperado.", string detail = null)
    {
        var errorDetails = ErrorDetails.ErrorResponse(StatusCodes.Status500InternalServerError, message, null, detail);
        return Results.Problem(detail: detail, title: message, statusCode: StatusCodes.Status500InternalServerError);
    }
}