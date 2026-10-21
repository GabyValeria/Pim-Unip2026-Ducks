using System.Data;
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

    public async Task<int?> AutenticarUsuarioAsync(string email, string senhaHash)
    {
        using var connection = new SqlConnection(_connectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@Email", email);
        parameters.Add("@HashSenha", senhaHash);
        
        var resultado = await connection.QueryFirstOrDefaultAsync<int?>(
            "sp_AutenticarUsuario", 
            parameters, 
            commandType: CommandType.StoredProcedure);

        return resultado;
    }

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

    public async Task<UsuarioDto?> ObterDadosUsuarioPorEmailAsync(string email)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = "SELECT UsuarioID AS UsuarioId, NomeUsuario, Email FROM Usuarios WHERE Email = @Email AND EstaAtivo = 1";
        return await connection.QueryFirstOrDefaultAsync<UsuarioDto>(query, new { Email = email });
    }
}