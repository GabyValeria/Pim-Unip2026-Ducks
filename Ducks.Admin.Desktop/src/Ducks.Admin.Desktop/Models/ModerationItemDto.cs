using System;

namespace Ducks.Admin.Desktop.Models
{
    public class ModerationItemDto
    {
        public int ItemID { get; set; }
        public string TipoConteudo { get; set; } = string.Empty; // Trilha, Aula, Comentario
        public string Conteudo { get; set; } = string.Empty;
        public string Autor { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDENTE"; // PENDENTE, APROVADO, REJEITADO
        public DateTime CriadoEm { get; set; }
    }

    public class AutorDto
    {
        public int UsuarioID { get; set; }
        public string Nome { get; set; } = string.Empty;
    }
}