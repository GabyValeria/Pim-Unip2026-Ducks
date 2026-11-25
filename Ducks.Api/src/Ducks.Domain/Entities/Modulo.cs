namespace Ducks.Domain.Entities;

public class Modulo
{
    public int ModuloID { get; set; }
    public int TrilhaID { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int IndiceOrdem { get; set; }
    public string Status { get; set; } = StatusConteudo.Rascunho;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
