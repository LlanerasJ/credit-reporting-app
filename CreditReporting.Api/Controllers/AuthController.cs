using CreditReporting.Api.Services;
using CreditReporting.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CreditReporting.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Exchanges username/password for a JWT (demo users: analyst/Demo123!, admin/Admin123!).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new ProblemDetails { Title = "Username and password are required." });

        var response = await _auth.LoginAsync(request, ct);
        return response is null
            ? Unauthorized(new ProblemDetails { Title = "Invalid username or password." })
            : Ok(response);
    }

    /// <summary>Changes the signed-in user's password after verifying the current one.</summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new ProblemDetails { Title = "Current and new password are required." });
        if (request.NewPassword.Length < 8)
            return BadRequest(new ProblemDetails { Title = "New password must be at least 8 characters." });

        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new ProblemDetails { Title = "Not authorized. Please log in again." });

        bool changed = await _auth.ChangePasswordAsync(username, request.CurrentPassword, request.NewPassword, ct);
        return changed
            ? NoContent()
            : Unauthorized(new ProblemDetails { Title = "Current password is incorrect." });
    }
}
