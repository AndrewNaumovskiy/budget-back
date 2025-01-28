using Budget.API.Models;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;
using Telegram.Bot.Types;

namespace Budget.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        HttpResponse resp = httpContext.Response;

        resp.ContentType = "application/json";
        resp.StatusCode = (int)HttpStatusCode.InternalServerError;

        ResponseModel<IData, Error> respModel = new ResponseModel<IData, Error>()
        {
            Error = new Error(exception.Message)
        };

        return resp.WriteAsJsonAsync(respModel);
    }
}
