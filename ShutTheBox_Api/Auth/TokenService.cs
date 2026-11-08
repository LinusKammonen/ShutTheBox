using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ShutTheBox_Api.Auth;

public class TokenService
{
    public const string UserIdClaim = "sub";
    public const string UserNameClaim = "name";

    private readonly JwtOptions options;

    public TokenService(IOptions<JwtOptions> options)
    {
        this.options = options.Value;
    }

    public static SymmetricSecurityKey CreateSigningKey(string key) =>
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

    public string CreateToken(string userId, string username)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = DateTime.UtcNow.AddHours(options.ExpiresHours),
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(UserIdClaim, userId),
                new Claim(UserNameClaim, username)
            }),
            SigningCredentials = new SigningCredentials(CreateSigningKey(options.Key), SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
