using System.Data;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Ducks.Api.Services;

public class UsuarioDto
{
    public int UsuarioId { get; set; }
    public string NomeUsuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class EduDatabaseService
{
    private readonly string _connectionString;

    public EduDatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");
    }

    // --- FUNÇÃO AUXILIAR DE CRIPTOGRAFIA SHA-256 NA API ---
    private string ObterHashSha256(string texto)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("X2")); // Formato hexadecimal em maiúsculo (compatível com o SQL Server)
        }
        return sb.ToString();
    }

    // --- AUTENTICAÇÃO E USUÁRIO (Criptografa a senha na API antes de enviar ao banco) ---
    public async Task<UsuarioDto?> AutenticarEObterUsuarioAsync(string email, string senhaPlainText)
    {
        // 1. Criptografa a senha usando SHA-256 diretamente na API
        string senhaHash = ObterHashSha256(senhaPlainText);

        using var connection = new SqlConnection(_connectionString);
        var query = @"
            SELECT TOP 1 
                UsuarioID AS UsuarioId, 
                NomeUsuario, 
                Email 
            FROM Usuarios 
            WHERE (Email = @Email OR NomeUsuario = @Email) 
              AND SenhaHash = @SenhaHash 
              AND EstaAtivo = 1";

        return await connection.QueryFirstOrDefaultAsync<UsuarioDto>(
            query, 
            new { Email = email.Trim(), SenhaHash = senhaHash }
        );
    }

    public async Task<UsuarioDto?> ObterDadosUsuarioPorEmailAsync(string email)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = "SELECT UsuarioID AS UsuarioId, NomeUsuario, Email FROM Usuarios WHERE Email = @Email AND EstaAtivo = 1";
        return await connection.QueryFirstOrDefaultAsync<UsuarioDto>(query, new { Email = email.Trim() });
    }

    public async Task<List<string>> ObterPapeisUsuarioAsync(int usuarioId)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = @"
            SELECT p.NomePapel 
            FROM UsuarioPapeis up
            INNER JOIN Papeis p ON up.PapelID = p.PapelID
            WHERE up.UsuarioID = @UsuarioId";

        var papeis = await connection.QueryAsync<string>(query, new { UsuarioId = usuarioId });
        return papeis.AsList();
    }

    // --- SUBMISSÃO E PROVAS (Stored Procedures Transacionais) ---
    public async Task SubmeterRespostaProvaAsync(int provaUsuarioId, int questaoId, int alternativaId)
    {
        using var connection = new SqlConnection(_connectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@ProvaUsuarioID", provaUsuarioId);
        parameters.Add("@QuestaoID", questaoId);
        parameters.Add("@AlternativaID", alternativaId);

        await connection.ExecuteAsync(
            "sp_SubmeterRespostaProva", 
            parameters, 
            commandType: CommandType.StoredProcedure);
    }

    // --- RECURSOS DE GAMIFICAÇÃO E RECOMPENSAS ---
    public async Task VerificarEConcederConquistasAsync(int usuarioId)
    {
        using var connection = new SqlConnection(_connectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@UsuarioID", usuarioId);

        await connection.ExecuteAsync(
            "sp_VerificarEConcederConquistas", 
            parameters, 
            commandType: CommandType.StoredProcedure);
    }

    public async Task<object?> ObterXPUsuarioAsync(int usuarioId)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = @"
            SELECT x.UsuarioID, x.TotalXP, n.NomeNivel AS NivelAtual, x.AtualizadoEm
            FROM XPUsuario x
            INNER JOIN NiveisGamificacao n x.NivelAtualID = n.NivelID
            WHERE x.UsuarioID = @UsuarioId";

        return await connection.QueryFirstOrDefaultAsync<object>(query, new { UsuarioId = usuarioId });
    }

    public async Task<IEnumerable<object>> ObterConquistasUsuarioAsync(int usuarioId)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = @"
            SELECT c.ConquistaID, c.NomeConquista, c.Descricao, cu.ConquistadoEm
            FROM ConquistasUsuario cu
            INNER JOIN Conquistas c ON cu.ConquistaID = c.ConquistaID
            WHERE cu.UsuarioID = @UsuarioId";

        return await connection.QueryAsync<object>(query, new { UsuarioId = usuarioId });
    }
}