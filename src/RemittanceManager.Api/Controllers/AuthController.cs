using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RemittanceManager.Application.Auth;
using RemittanceManager.Application.Auth.Dtos;

namespace RemittanceManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    private readonly IAuthService _auth = auth;

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var id = await _auth.RegisterAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, new { userId = id });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var response = await _auth.LoginAsync(request, ct);
        return Ok(response);
    }
}
