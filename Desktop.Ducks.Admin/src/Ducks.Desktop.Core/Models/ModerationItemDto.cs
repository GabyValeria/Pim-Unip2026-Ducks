using System;

namespace Ducks.Desktop.Core.Models
{
    public class ModerationItemDto
    {
        public int ItemID { get; set; }
        public string TipoConteudo { get; set; } = string.Empty; // "Curso", "Comentario", "Denuncia"
        public string TituloOuDescricao { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public DateTime DataEnvio { get; set; }
        public string Status { get; set; } = "Pendente";
    }
}