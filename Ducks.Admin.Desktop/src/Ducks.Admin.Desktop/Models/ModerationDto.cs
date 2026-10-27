using System;

namespace Ducks.Admin.Desktop.Models
{
    public class ModerationItemDto
    {
        public int ItemID { get; set; }
        public string TipoConteudo { get; set; } = string.Empty; // Trilha, Aula, Comentario
        public string Titulo { get; set; } = string.Empty;
        AutorDto Autor { get; set; } = new();
        public string Status { get; set; } = "PENDENTE"; // PENDENTE, APROVADO, REJEITADO
        public DateTime CriadoEm { get; set; }
    }

    public class AutorDto
    {
        public int UsuarioID { get; set; }
        public string Nome { get; set; } = string.Empty;
    }
}