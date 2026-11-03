using System;

namespace Ducks.Desktop.Core.Models
{
    public class BadgeDto
    {
        public int ConquistaID { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string IconeUrl { get; set; } = string.Empty;
        public int RecompensaXP { get; set; }
    }

    public class ChallengeDto
    {
        public int DesafioID { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int RecompensaXP { get; set; }
        public bool Ativo { get; set; }
    }

    public class UserXpAdjustmentDto
    {
        public int UsuarioID { get; set; }
        public int QuantidadeXP { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}