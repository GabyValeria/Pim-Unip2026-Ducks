using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ducks.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarUsuarioDto dto)
    {
        var resposta = await _authService.RegistrarAsync(dto);
        return Ok(resposta);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var resposta = await _authService.LoginAsync(dto);
        return Ok(resposta);
    }
}