using System.Security.Claims;

namespace ShutTheBox_Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(TokenService.UserIdClaim)
        ?? throw new InvalidOperationException("Användaren saknar userId i sin token.");

    public static string GetUserName(this ClaimsPrincipal user) =>
        user.FindFirstValue(TokenService.UserNameClaim) ?? "";
}
