namespace Ducks.Domain.Entities;

/// <summary>Inscrição de um estudante em um desafio (uma por usuário e desafio).</summary>
public class ParticipacaoDesafio
{
    public int ParticipacaoID { get; set; }
    public int DesafioID { get; set; }
    public int UsuarioID { get; set; }
    public DateTime InscritoEm { get; set; } = DateTime.UtcNow;
    public DateTime? ConcluidoEm { get; set; }
    public string Status { get; set; } = StatusParticipacao.EmAndamento;
}

public static class StatusParticipacao
{
    public const string EmAndamento = "EM_ANDAMENTO";
    public const string Concluido = "CONCLUIDO";
}

/// <summary>Requisito que o estudante precisa cumprir para concluir um desafio.</summary>
public static class TiposRequisitoDesafio
{
    public const string Nenhum = "NENHUM";
    public const string AulasConcluidas = "AULAS_CONCLUIDAS";
    public const string SimuladosAprovados = "SIMULADOS_APROVADOS";
    public const string TrilhasConcluidas = "TRILHAS_CONCLUIDAS";
    public const string XPTotal = "XP_TOTAL";
    public static readonly string[] Todos = [Nenhum, AulasConcluidas, SimuladosAprovados, TrilhasConcluidas, XPTotal];
}
