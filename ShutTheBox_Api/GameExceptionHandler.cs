using Microsoft.AspNetCore.Diagnostics;
using ShutTheBox_GameEngine;

namespace ShutTheBox_Api;

public class GameExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetails;

    public GameExceptionHandler(IProblemDetailsService problemDetails)
    {
        this.problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not GameException)
        {
            return false;
        }

        int status = exception is NotFoundException ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
        httpContext.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = status,
                Title = status == StatusCodes.Status404NotFound ? "Hittades inte" : "Ogiltig begäran",
                Detail = exception.Message
            }
        });
    }
}
