namespace Ducks.Admin.Desktop.Models;

public class ConquistaAdminDto
{
    public int ConquistaId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int XpNecessario { get; set; }
}

public class CriarConquistaRequestDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int XpNecessario { get; set; }
}