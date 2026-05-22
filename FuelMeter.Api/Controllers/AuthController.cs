using FuelMeter.Api.Services;
using FuelMeter.Core.DTOs;
using FuelMeter.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FuelMeter.Api.Controllers;

/// <summary>Authentication — register, login, password reset.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(IAuthService authService, JwtTokenService jwtService) : ControllerBase
{
    /// <summary>Register a new user account.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResult>> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        if (!result.Success) return BadRequest(result);

        if (result.User is not null)
            result.Token = jwtService.CreateToken(result.User);

        return Ok(result);
    }

    /// <summary>Login and receive a JWT bearer token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        if (!result.Success) return Unauthorized(result);

        if (result.User is not null)
            result.Token = jwtService.CreateToken(result.User);

        return Ok(result);
    }

    /// <summary>Request a password-reset email. Always returns 200 to prevent email enumeration.</summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResult>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await authService.ForgotPasswordAsync(request);
        return Ok(result);
    }

    /// <summary>Reset password using the token that was emailed to the user.</summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResult>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await authService.ResetPasswordAsync(request.Token, request.NewPassword);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
