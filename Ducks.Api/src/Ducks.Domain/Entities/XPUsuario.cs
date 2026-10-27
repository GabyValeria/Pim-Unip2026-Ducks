using System;

namespace Ducks.Domain.Entities;

public class XPUsuario
{
    public int UsuarioID { get; set; }
    public int TotalXP { get; set; }
    public int NivelAtualID { get; set; }
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}