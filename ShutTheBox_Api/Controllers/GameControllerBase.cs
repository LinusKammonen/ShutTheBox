using Microsoft.AspNetCore.Mvc;
using ShutTheBox_GameEngine;

namespace ShutTheBox_Api.Controllers;

public abstract class GameControllerBase : ControllerBase
{
    protected ActionResult<T> Run<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (GameException error)
        {
            return ToProblem(error);
        }
    }

    protected async Task<ActionResult<T>> RunAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (GameException error)
        {
            return ToProblem(error);
        }
    }

    private ObjectResult ToProblem(GameException error)
    {
        bool notFound = error is NotFoundException;

        return Problem(
            title: notFound ? "Hittades inte" : "Ogiltig begäran",
            detail: error.Message,
            statusCode: notFound ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
    }
}
