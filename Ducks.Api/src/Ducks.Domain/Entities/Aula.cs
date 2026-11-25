using System;

namespace Ducks.Domain.Entities;

public class Aula
{
    public int AulaID { get; set; }
    public int ModuloID { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string UrlConteudo { get; set; } = string.Empty;
    public int IndiceOrdem { get; set; }
    public string Status { get; set; } = "APROVADO";

    // Acrescentados na etapa de módulos e aulas (texto da aula e data da última alteração).
    public string? Conteudo { get; set; }
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}