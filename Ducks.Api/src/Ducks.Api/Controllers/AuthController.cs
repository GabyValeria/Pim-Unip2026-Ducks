using System.Security.Cryptography;
using System.Text;
using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ducks.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private const string RefreshCookie = "ducks.refresh";
    private const string CsrfCookie = "ducks.csrf";

    [HttpPost("registrar")]
    [EnableRateLimiting("AuthPerIp")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarUsuarioDto dto, CancellationToken cancellationToken)
    {
        var result = await authService.RegistrarAsync(dto, Request.Headers["User-Agent"].ToString(), cancellationToken);
        return Ok(PrepareWebSession(result));
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthPerIp")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(dto, Request.Headers["User-Agent"].ToString(), cancellationToken);
        return Ok(PrepareWebSession(result));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting("AuthPerIp")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto, CancellationToken cancellationToken)
    {
        var token = ResolveRefreshToken(dto.RefreshToken);
        if (token is null) return Unauthorized();
        var result = await authService.RenovarAsync(token, Request.Headers["User-Agent"].ToString(), cancellationToken);
        return Ok(PrepareWebSession(result));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [EnableRateLimiting("AuthPerIp")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto, CancellationToken cancellationToken)
    {
        var token = ResolveRefreshToken(dto.RefreshToken);
        if (token is null) return Unauthorized();
        await authService.RevogarAsync(token, cancellationToken);
        ClearWebSession();
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("csrf")]
    public IActionResult GetCsrfToken()
    {
        if (!IsWebClient()) return BadRequest(new { message = "Este endpoint é destinado ao cliente Web." });
        var token = Request.Cookies[CsrfCookie];
        if (string.IsNullOrWhiteSpace(token))
        {
            token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            Response.Cookies.Append(CsrfCookie, token, CookieOptionsForWeb(httpOnly: false));
        }
        return Ok(new { csrfToken = token });
    }

    private AuthRespostaDto PrepareWebSession(AuthRespostaDto result)
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Pragma"] = "no-cache";
        if (!IsWebClient()) return result;

        var csrf = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Response.Cookies.Append(RefreshCookie, result.RefreshToken, CookieOptionsForWeb(httpOnly: true, expires: result.RefreshTokenExpiracao));
        Response.Cookies.Append(CsrfCookie, csrf, CookieOptionsForWeb(httpOnly: false, expires: result.RefreshTokenExpiracao));
        result.RefreshToken = string.Empty; // navegador recebe o refresh token somente em cookie HttpOnly
        result.CsrfToken = csrf;
        return result;
    }

    private string? ResolveRefreshToken(string? bodyToken)
    {
        if (!IsWebClient()) return bodyToken;

        var cookieToken = Request.Cookies[CsrfCookie];
        var headerToken = Request.Headers["X-CSRF-Token"].ToString();
        if (string.IsNullOrWhiteSpace(cookieToken) || string.IsNullOrWhiteSpace(headerToken))
            return null;
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(cookieToken), Encoding.UTF8.GetBytes(headerToken)))
            return null;
        return Request.Cookies[RefreshCookie];
    }

    private bool IsWebClient() => string.Equals(
        Request.Headers["X-Client-Platform"].ToString(), "web", StringComparison.OrdinalIgnoreCase);

    private static CookieOptions CookieOptionsForWeb(bool httpOnly, DateTime? expires = null) => new()
    {
        HttpOnly = httpOnly,
        Secure = true,
        SameSite = SameSiteMode.None,
        Path = "/api/auth",
        IsEssential = true,
        Expires = expires.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(expires.Value, DateTimeKind.Utc))
            : null
    };

    private void ClearWebSession()
    {
        Response.Cookies.Delete(RefreshCookie, CookieOptionsForWeb(httpOnly: true));
        Response.Cookies.Delete(CsrfCookie, CookieOptionsForWeb(httpOnly: false));
    }
}
