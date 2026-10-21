using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Ducks.Api.Services;

public class EduDatabaseService
{
    private readonly string _connectionString;

    public EduDatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");
    }

    // Exemplo de integração segura com a stored procedure sp_AutenticarUsuario
    public async Task<int?> AutenticarUsuarioAsync(string email, string senhaHash)
    {
        using var connection = new SqlConnection(_connectionString);
        var parameters = new DynamicParameters();
        parameters.Add("@Email", email);
        parameters.Add("@HashSenha", senhaHash);
        
        // Retorna o UsuarioID se bem-sucedido ou null
        var resultado = await connection.QueryFirstOrDefaultAsync<int?>(
            "sp_AutenticarUsuario", 
            parameters, 
            commandType: CommandType.StoredProcedure);

        return resultado;
    }

    // Exemplo de submissão transacional de prova e atribuição de XP (sp_SubmeterRespostaProva)
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
}