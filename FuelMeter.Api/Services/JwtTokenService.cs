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

    public string CreateToken(UserDto user)
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

        var token = new JwtSecurityToken(
            issuer:             _opts.Issuer,
            audience:           _opts.Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_opts.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
