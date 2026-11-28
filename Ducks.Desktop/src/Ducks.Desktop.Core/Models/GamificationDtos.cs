using System;

namespace Ducks.Desktop.Core.Models
{
    /// <summary>Medalha (Conquista) de GET api/admin/gamification/badges.</summary>
    public class BadgeDto
    {
        public int ConquistaID { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int XPNecessario { get; set; }
        public bool Ativa { get; set; } = true;
        public int TotalConcedidas { get; set; }
    }

    /// <summary>Corpo de POST/PUT badges (ConquistaDto da API).</summary>
    public class BadgeRequestDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int XPNecessario { get; set; }
        public bool Ativa { get; set; } = true;
    }

    public class UserBadgeDto
    {
        public int ConquistaID { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public DateTime ConquistadaEm { get; set; }
    }

    /// <summary>Desafio de GET api/admin/gamification/challenges.</summary>
    public class ChallengeDto
    {
        public int DesafioID { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int RecompensaXP { get; set; }
        public DateTime? InicioEm { get; set; }
        public DateTime? FimEm { get; set; }
        public bool Ativo { get; set; }
        public string? TipoRequisito { get; set; }
        public int MetaRequisito { get; set; }
        public int? TrilhaID { get; set; }
        public int? ConquistaID { get; set; }
    }

    /// <summary>Corpo de POST/PUT challenges (DesafioDto da API).</summary>
    public class ChallengeRequestDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int RecompensaXP { get; set; }
        public DateTime? InicioEm { get; set; }
        public DateTime? FimEm { get; set; }
        public bool Ativo { get; set; } = true;
        public string? TipoRequisito { get; set; }
        public int MetaRequisito { get; set; }
        public int? TrilhaID { get; set; }
        public int? ConquistaID { get; set; }
    }

    public static class TiposRequisito
    {
        public static readonly string[] Todos =
        {
            "NENHUM", "AULAS_CONCLUIDAS", "SIMULADOS_APROVADOS", "TRILHAS_CONCLUIDAS", "XP_TOTAL"
        };
    }
}
