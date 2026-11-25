using System.ComponentModel.DataAnnotations.Schema;

namespace Ducks.Domain.Entities;

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public int UsuarioID { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid TokenFamilyId { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime ExpiraEm { get; set; }
    public DateTime? RevogadoEm { get; set; }
    public Guid? SubstituidoPorId { get; set; }
    public string? Dispositivo { get; set; }
    public byte[] RowVersion { get; set; } = [];

    [NotMapped]
    public string RawToken { get; set; } = string.Empty;
}
