using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Ducks.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly DucksDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<Usuario> _passwordHasher;

    public AuthService(DucksDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<Usuario>();
    }

    public async Task<AuthRespostaDto> RegistrarAsync(RegistrarUsuarioDto dto)
    {
        var emailExiste = await _context.Usuarios.AnyAsync(u => u.Email == dto.Email);
        if (emailExiste)
        {
            throw new InvalidOperationException("E-mail já cadastrado na plataforma.");
        }

        var usuario = new Usuario
        {
            NomeUsuario = dto.NomeUsuario,
            Email = dto.Email,
            SaltSenha = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
            CriadoEm = DateTime.UtcNow,
            EstaAtivo = true
        };

        usuario.HashSenha = _passwordHasher.HashPassword(usuario, dto.Senha);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        var tokenInfo = GerarJwtToken(usuario);

        return new AuthRespostaDto
        {
            UsuarioID = usuario.UsuarioID,
            NomeUsuario = usuario.NomeUsuario,
            Email = usuario.Email,
            Token = tokenInfo.Token,
            Expiracao = tokenInfo.Expiracao
        };
    }

    public async Task<AuthRespostaDto> LoginAsync(LoginDto dto)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (usuario == null || !usuario.EstaAtivo)
        {
            throw new UnauthorizedAccessException("Credenciais inválidas ou conta inativa.");
        }

        var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.HashSenha, dto.Senha);
        if (resultado == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        var tokenInfo = GerarJwtToken(usuario);

        return new AuthRespostaDto
        {
            UsuarioID = usuario.UsuarioID,
            NomeUsuario = usuario.NomeUsuario,
            Email = usuario.Email,
            Token = tokenInfo.Token,
            Expiracao = tokenInfo.Expiracao
        };
    }

    private (string Token, DateTime Expiracao) GerarJwtToken(Usuario usuario)
    {
        var jwtSecret = _configuration["Jwt:Secret"] ?? "DucksPlatformSecretKey_Minimo32Caracteres!";
        var key = Encoding.ASCII.GetBytes(jwtSecret);
        var expiracao = DateTime.UtcNow.AddHours(8);

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioID.ToString()),
                new Claim(ClaimTypes.Name, usuario.NomeUsuario),
                new Claim(ClaimTypes.Email, usuario.Email)
            }),
            Expires = expiracao,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiracao);
    }
}