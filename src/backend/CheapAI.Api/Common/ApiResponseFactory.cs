using CheapAI.Application.Common.Responses;

namespace CheapAI.Api.Common;

public static class ApiResponseFactory
{
    public static ApiEnvelope<T> Success<T>(HttpContext httpContext, T data, string message = "ok")
    {
        return new ApiEnvelope<T>
        {
            Code = 0,
            Message = message,
            Data = data,
            RequestId = httpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiEnvelope<object?> Success(HttpContext httpContext, string message = "ok")
    {
        return Success<object?>(httpContext, null, message);
    }

    public static ApiEnvelope<T> Failure<T>(HttpContext httpContext, int code, string message, T data)
    {
        return new ApiEnvelope<T>
        {
            Code = code,
            Message = message,
            Data = data,
            RequestId = httpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };
    }
}
