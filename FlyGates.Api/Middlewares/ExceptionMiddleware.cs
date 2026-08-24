using System.Net;
using FlyGates.Application.Exceptions;
using FlyGates.Model;
using Newtonsoft.Json;

namespace FlyGates.Middlewares;

public class ExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BaseException ex)
        {
            await HandleExceptionAsync(context, ex);
        }
        catch (Exception e)
        {
            await HandleExceptionAsync(context, new BaseException($"An unexpected error occurred. {e}", (int)HttpStatusCode.InternalServerError));
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, BaseException exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = exception.StatusCode;

        ErrorResponse response = new()
        {
            Message = exception.Message,
            StatusCode = exception.StatusCode
        };

        string jsonResponse = JsonConvert.SerializeObject(response);
        return context.Response.WriteAsync(jsonResponse);
    }
}
