using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

namespace Ducks.IntegrationTests;

/// <summary>
/// Só roda quando a variável DUCKS_TEST_SQL aponta para um SQL Server de TESTE
/// (ex.: Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=True).
/// Sem ela, os testes de integração aparecem como "ignorados".
/// </summary>
public sealed class FactSqlAttribute : FactAttribute
{
    public FactSqlAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DUCKS_TEST_SQL")))
            Skip = "Defina DUCKS_TEST_SQL para rodar os testes de integração com SQL Server.";
    }
}

/// <summary>
/// Cria um banco novo (DucksTeste_xxxx) com Database/01 e 02, sobe a API apontando para ele
/// e apaga o banco no final. Nunca usa a conexão do .env nem a do Azure.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public const string JwtSecret = "segredo-somente-para-testes-de-integracao-0123456789";
    private const string Issuer = "Ducks.Api";
    private const string Audience = "Ducks.Clients";
    private string _master = string.Empty;
    public string NomeBanco { get; } = "DucksTeste_" + Guid.NewGuid().ToString("N")[..8];
    public string ConnectionString { get; private set; } = string.Empty;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var baseConn = Environment.GetEnvironmentVariable("DUCKS_TEST_SQL");
        if (string.IsNullOrWhiteSpace(baseConn)) return;
        _master = new SqlConnectionStringBuilder(baseConn) { InitialCatalog = "master" }.ConnectionString;
        ConnectionString = new SqlConnectionStringBuilder(baseConn) { InitialCatalog = NomeBanco }.ConnectionString;
        await ExecutarAsync(_master, $"CREATE DATABASE [{NomeBanco}]");
        foreach (var script in new[] { "01_esquema_base.sql", "02_novas_tabelas.sql" })
            await ExecutarScriptAsync(Path.Combine(AppContext.BaseDirectory, "Database", script));

        // Variáveis de ambiente têm prioridade sobre appsettings e são lidas no topo do Program.cs.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Secret", JwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", Audience);
        Environment.SetEnvironmentVariable("OpenAI__ApiKey", "");
        Environment.SetEnvironmentVariable("RateLimiting__Auth__PermitLimit", "1000");
        Environment.SetEnvironmentVariable("RateLimiting__RecuperacaoSenha__PermitLimit", "1000");
        Environment.SetEnvironmentVariable("Email__Provedor", "Mock");
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        });
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrWhiteSpace(_master)) return;
        await Factory.DisposeAsync();
        SqlConnection.ClearAllPools();
        await ExecutarAsync(_master, $"ALTER DATABASE [{NomeBanco}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{NomeBanco}]");
    }

    /// <summary>Cria um usuário direto no banco com o papel indicado e devolve o ID.</summary>
    public async Task<int> CriarUsuarioAsync(string papel, string? email = null)
    {
        email ??= $"{papel.ToLowerInvariant()}_{Guid.NewGuid():N}@teste.com";
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand("""
            INSERT INTO dbo.Usuarios (NomeUsuario, Email, SenhaHash, EstaAtivo, CriadoEm) VALUES (@n, @e, N'sem-senha', 1, SYSUTCDATETIME());
            DECLARE @id int = SCOPE_IDENTITY();
            INSERT INTO dbo.UsuarioPapeis (UsuarioID, PapelID, AtribuidoEm) SELECT @id, PapelID, SYSUTCDATETIME() FROM dbo.Papeis WHERE NomePapel = @p;
            SELECT @id;
            """, conn);
        cmd.Parameters.AddWithValue("@n", email.Split('@')[0][..Math.Min(40, email.Split('@')[0].Length)]);
        cmd.Parameters.AddWithValue("@e", email);
        cmd.Parameters.AddWithValue("@p", papel);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    /// <summary>Cliente HTTP autenticado com um JWT igual ao que a API emite (mesmo segredo, emissor e papel).</summary>
    public HttpClient Cliente(int usuarioId, string papel)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(ClaimTypes.Name, $"usuario{usuarioId}"),
            new Claim(ClaimTypes.Role, papel)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var jwt = new JwtSecurityToken(Issuer, Audience, claims, DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddMinutes(15),
            new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        return client;
    }

    public async Task<int> AutenticadoAsync(string papel) => await CriarUsuarioAsync(papel);

    public async Task<object?> ConsultarAsync(string sql)
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn);
        return await cmd.ExecuteScalarAsync();
    }

    public async Task ExecutarAsync(string sql) => await ExecutarAsync(ConnectionString, sql);

    private static async Task ExecutarAsync(string conexao, string sql)
    {
        await using var conn = new SqlConnection(conexao);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task ExecutarScriptAsync(string caminho)
    {
        // Mesma sessão para o script inteiro (os procedimentos temporários # dependem disso).
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        foreach (var lote in Regex.Split(await File.ReadAllTextAsync(caminho), @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(lote)) continue;
            await using var cmd = new SqlCommand(lote, conn) { CommandTimeout = 120 };
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;
