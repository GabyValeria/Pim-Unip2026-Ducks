namespace Ducks.Domain.Entities;

/// <summary>Pedido de recuperação de senha. Só o hash SHA-256 do token é guardado.</summary>
public class TokenRecuperacaoSenha
{
    public int TokenID { get; set; }
    public int UsuarioID { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime ExpiraEm { get; set; }
    public DateTime? UsadoEm { get; set; }
}
