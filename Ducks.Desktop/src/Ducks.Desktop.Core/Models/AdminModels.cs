using System;

namespace Ducks.Desktop.Core.Models
{
    public class RoleDto
    {
        public int PapelID { get; set; }
        public string NomePapel { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
    }

    public class AssignRoleRequestDto
    {
        public int UsuarioID { get; set; }
        public int PapelID { get; set; }
    }

    public class AuditLogDto
    {
        public int LogID { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public string Detalhes { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
    }

    public class SettingDto
    {
        public string Chave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public DateTime? AtualizadoEm { get; set; }
    }

    public class UpdateSettingRequestDto
    {
        public string Chave { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
    }

    public class TrilhaDto
    {
        public int TrilhaID { get; set; }
        public string NomeTrilha { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int MateriaID { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? AtualizadoEm { get; set; }
    }

    public class CreateTrilhaRequestDto
    {
        public string NomeTrilha { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int MateriaID { get; set; }
    }

    public class AiPromptRequestDto
    {
        public string Contexto { get; set; } = string.Empty;
        public string TarefaAdmin { get; set; } = string.Empty;
    }

    public class AiResponseDto
    {
        public string ResultadoGerado { get; set; } = string.Empty;
        public bool Sucesso { get; set; }
        public string MensagemErro { get; set; } = string.Empty;
    }
}
