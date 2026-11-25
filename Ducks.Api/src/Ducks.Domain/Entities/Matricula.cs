namespace Ducks.Domain.Entities;

/// <summary>Matrícula do usuário em uma trilha (tabela UsuarioTrilhas do banco do grupo).</summary>
public class UsuarioTrilha
{
    public int UsuarioTrilhaID { get; set; }
    public int UsuarioID { get; set; }
    public int TrilhaID { get; set; }
    public DateTime InscritoEm { get; set; } = DateTime.UtcNow;
    public DateTime? ConcluidoEm { get; set; }
    public decimal ProgressoPercentual { get; set; }
    public string Status { get; set; } = StatusMatricula.Ativa;
    public DateTime? CanceladoEm { get; set; }
}

/// <summary>Mesmos valores da restrição CK_StatusUsuarioTrilha do banco do grupo.</summary>
public static class StatusMatricula
{
    public const string Ativa = "EM_ANDAMENTO";
    public const string Concluida = "CONCLUIDA";
    public const string Cancelada = "CANCELADA";
}

/// <summary>Conclusão de uma aula pelo usuário (uma linha por usuário e aula).</summary>
public class ProgressoUsuario
{
    public int ProgressoID { get; set; }
    public int UsuarioID { get; set; }
    public int AulaID { get; set; }
    public DateTime ConcluidoEm { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Registro de recompensa já paga. A chave única (UsuarioID, TipoOrigem, OrigemID)
/// impede pontuar duas vezes o mesmo evento (idempotência).
/// </summary>
public class RecompensaConcedida
{
    public int RecompensaID { get; set; }
    public int UsuarioID { get; set; }
    public string TipoOrigem { get; set; } = string.Empty;
    public int OrigemID { get; set; }
    public int QuantidadeXP { get; set; }
    public int? ConquistaID { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public static class OrigemRecompensa
{
    public const string Aula = "AULA";
    public const string Trilha = "TRILHA";
    public const string Prova = "PROVA";
    public const string Desafio = "DESAFIO";
}
