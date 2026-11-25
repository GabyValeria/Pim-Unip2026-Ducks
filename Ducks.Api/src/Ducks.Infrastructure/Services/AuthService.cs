using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Ducks.Infrastructure.Services;

public class AuthService : IAuthService
{
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14);
    private readonly DucksDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public AuthService(DucksDbContext context, IConfiguration configuration, IPasswordHasher<Usuario> passwordHasher)
    {
        _context = context;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthRespostaDto> RegistrarAsync(RegistrarUsuarioDto dto, string? device, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim();
        if (await _context.Usuarios.AnyAsync(u => u.Email == email, cancellationToken))
            throw new ConflictException("E-mail já cadastrado na plataforma.");

        var now = DateTime.UtcNow;
        var usuario = new Usuario
        {
            NomeUsuario = dto.NomeUsuario.Trim(),
            Email = email,
            CriadoEm = now,
            AtualizadoEm = now,
            EstaAtivo = true
        };
        usuario.SenhaHash = _passwordHasher.HashPassword(usuario, dto.Senha);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);
        // O papel padrão é atribuído no servidor. Nenhum papel administrativo é atribuído no cadastro.
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO dbo.UsuarioPapeis (UsuarioID, PapelID, AtribuidoEm) SELECT {usuario.UsuarioID}, p.PapelID, SYSUTCDATETIME() FROM dbo.Papeis p WHERE p.NomePapel IN ('Estudante', 'Aluno') AND NOT EXISTS (SELECT 1 FROM dbo.UsuarioPapeis up WHERE up.UsuarioID = {usuario.UsuarioID} AND up.PapelID = p.PapelID)",
            cancellationToken);
        var refresh = CreateRefreshToken(usuario.UsuarioID, Guid.NewGuid(), device);
        _context.RefreshTokens.Add(refresh);
        await _context.SaveChangesAsync(cancellationToken);
        var result = await CriarRespostaAsync(usuario, refresh, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<AuthRespostaDto> LoginAsync(LoginDto dto, string? device, CancellationToken cancellationToken = default)
    {
        var email = dto.Email.Trim();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (usuario is null || !usuario.EstaAtivo)
            throw new UnauthorizedAccessException();

        var verification = _passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash, dto.Senha);
        if (verification == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException();

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.SenhaHash = _passwordHasher.HashPassword(usuario, dto.Senha);
            usuario.AtualizadoEm = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
        return await CriarSessaoAsync(usuario, Guid.NewGuid(), device, cancellationToken);
    }

    public async Task<AuthRespostaDto> RenovarAsync(string refreshToken, string? device, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) throw new UnauthorizedAccessException();
        var hash = Hash(refreshToken);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is null) throw new UnauthorizedAccessException();

        if (stored.RevogadoEm is not null)
        {
            var family = await _context.RefreshTokens
                .Where(t => t.TokenFamilyId == stored.TokenFamilyId && t.RevogadoEm == null)
                .ToListAsync(cancellationToken);
            foreach (var token in family) token.RevogadoEm = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new UnauthorizedAccessException("Reutilização de Refresh Token detectada. Sessão revogada.");
        }

        if (stored.ExpiraEm <= DateTime.UtcNow)
            throw new UnauthorizedAccessException();

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioID == stored.UsuarioID && u.EstaAtivo, cancellationToken);
        if (usuario is null) throw new UnauthorizedAccessException();

        stored.RevogadoEm = DateTime.UtcNow;
        var newToken = CreateRefreshToken(usuario.UsuarioID, stored.TokenFamilyId, device);
        stored.SubstituidoPorId = newToken.Id;
        _context.RefreshTokens.Add(newToken);
        await _context.SaveChangesAsync(cancellationToken);
        var result = await CriarRespostaAsync(usuario, newToken, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task RevogarAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var hash = Hash(refreshToken);
        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is null || stored.RevogadoEm is not null) return;
        stored.RevogadoEm = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthRespostaDto> CriarSessaoAsync(Usuario usuario, Guid familyId, string? device, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var refresh = CreateRefreshToken(usuario.UsuarioID, familyId, device);
        _context.RefreshTokens.Add(refresh);
        await _context.SaveChangesAsync(cancellationToken);
        var result = await CriarRespostaAsync(usuario, refresh, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<AuthRespostaDto> CriarRespostaAsync(Usuario usuario, RefreshToken refresh, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var expires = now.Add(AccessTokenLifetime);
        var roles = await _context.Database
            .SqlQuery<string>($"SELECT p.NomePapel AS [Value] FROM dbo.UsuarioPapeis up INNER JOIN dbo.Papeis p ON p.PapelID = up.PapelID WHERE up.UsuarioID = {usuario.UsuarioID}")
            .ToListAsync(cancellationToken);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.UsuarioID.ToString()),
            new(ClaimTypes.NameIdentifier, usuario.UsuarioID.ToString()),
            new(ClaimTypes.Name, usuario.NomeUsuario),
            new(ClaimTypes.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role == "Aluno" ? "Estudante" : role)));

        var secret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret não configurado.");
        var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer não configurado.");
        var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience não configurado.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var jwt = new JwtSecurityToken(issuer, audience, claims, now, expires,
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AuthRespostaDto
        {
            UsuarioID = usuario.UsuarioID,
            NomeUsuario = usuario.NomeUsuario,
            Email = usuario.Email,
            Token = new JwtSecurityTokenHandler().WriteToken(jwt),
            Expiracao = expires,
            RefreshToken = refresh.RawToken,
            RefreshTokenExpiracao = refresh.ExpiraEm
        };
    }

    private RefreshToken CreateRefreshToken(int usuarioId, Guid familyId, string? device)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioID = usuarioId,
            TokenHash = Hash(raw),
            TokenFamilyId = familyId,
            CriadoEm = DateTime.UtcNow,
            ExpiraEm = DateTime.UtcNow.Add(RefreshTokenLifetime),
            Dispositivo = string.IsNullOrWhiteSpace(device) ? null : device[..Math.Min(device.Length, 150)],
            // O token bruto é mantido somente no objeto durante a resposta; nunca é persistido.
            RawToken = raw
        };
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
