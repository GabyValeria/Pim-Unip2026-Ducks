using System;

namespace Ducks.Domain.Entities;

public class TrilhaAprendizagem
{
    public int TrilhaID { get; set; }
    public string NomeTrilha { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int MateriaID { get; set; }
    public string Status { get; set; } = "APROVADO";
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}