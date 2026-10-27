using System;

namespace Ducks.Domain.Entities;

public class HistoricoXP
{
    public int HistoricoXPID { get; set; }
    public int UsuarioID { get; set; }
    public int QuantidadeXP { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}