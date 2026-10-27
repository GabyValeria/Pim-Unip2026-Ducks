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
}