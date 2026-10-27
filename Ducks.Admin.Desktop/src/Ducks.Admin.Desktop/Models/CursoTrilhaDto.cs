namespace Ducks.Admin.Desktop.Models
{
    public class CursoTrilhaDto
    {
        public int CursoID { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public int CargaHoraria { get; set; }
        public bool Publicado { get; set; }
    }
}