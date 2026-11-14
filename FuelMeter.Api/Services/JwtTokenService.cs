using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FuelMeter.Api.Options;
using FuelMeter.Core.DTOs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FuelMeter.Api.Services;

public class JwtTokenService(IOptions<JwtOptions> opts)
{
    private readonly JwtOptions _opts = opts.Value;

    public (string Token, DateTimeOffset ExpiresAt) CreateToken(UserDto user, bool staySignedIn = false)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("fullName",                    user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString())
        };

        var now = DateTimeOffset.UtcNow;
        var expiresAt = staySignedIn
            ? now.AddDays(Math.Clamp(_opts.RememberedSessionDays, 1, 30))
            : now.AddMinutes(_opts.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer:             _opts.Issuer,
            audience:           _opts.Audience,
            claims:             claims,
            expires:            expiresAt.UtcDateTime,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
