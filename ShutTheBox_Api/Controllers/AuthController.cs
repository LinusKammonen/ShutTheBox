using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShutTheBox_Api.Auth;
using ShutTheBox_GameEngine;

namespace ShutTheBox_Api.Controllers;

[ApiController]
[Route("v1/auth")]
public class AuthController : GameControllerBase
{
    private readonly TokenService tokenService;
    private readonly UserStore userStore;

    public AuthController(TokenService tokenService, UserStore userStore)
    {
        this.tokenService = tokenService;
        this.userStore = userStore;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        string username = request.Username?.Trim() ?? "";

        if (username.Length < 2 || username.Length > 20)
        {
            return Problem(title: "Ogiltig begäran", detail: "Användarnamnet måste vara mellan 2 och 20 tecken.", statusCode: StatusCodes.Status400BadRequest);
        }

        string userId = userStore.GetOrCreateUserId(username);
        string token = tokenService.CreateToken(userId, username);

        return new LoginResponse(token, userId, username);
    }

    [Authorize]
    [HttpGet("me")]
    public MeResponse Me()
    {
        return new MeResponse(User.GetUserId(), User.GetUserName());
    }
}
