using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ducks.Api.DTOs;
using Ducks.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Ducks.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly EduDatabaseService _dbService;
    private readonly IConfiguration _configuration;

    public AuthController(EduDatabaseService dbService, IConfiguration configuration)
    {
        _dbService = dbService;
        _configuration = configuration;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var usuario = await _dbService.ObterDadosUsuarioPorEmailAsync(request.Email);
        if (usuario == null)
        {
            return Unauthorized(new { message = "Credenciais inválidas." });
        }

        var usuarioIdValidado = await _dbService.AutenticarUsuarioAsync(request.Email, request.Senha);
        if (!usuarioIdValidado.HasValue)
        {
            return Unauthorized(new { message = "E-mail ou senha incorretos." });
        }

        var papeis = await _dbService.ObterPapeisUsuarioAsync(usuario.UsuarioId);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["Secret"] ?? "ChaveSecretaPadraoSuperSeguraDucks2026!";
        var key = Encoding.ASCII.GetBytes(secretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.NomeUsuario)
        };

        foreach (var papel in papeis)
        {
            claims.Add(new Claim(ClaimTypes.Role, papel));
        }

        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "120");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(new LoginResponseDto
        {
            Token = tokenString,
            ExpiraEmMinutos = expirationMinutes,
            NomeUsuario = usuario.NomeUsuario,
            Papeis = papeis
        });
    }
}