namespace Ducks.Admin.Desktop.Models;

public class UsuarioAdminDto
{
    public int UsuarioId { get; set; }
    public string NomeUsuario { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EstaAtivo { get; set; }
    public DateTime CriadoEm { get; set; }
    public List<string> Papeis { get; set; } = new();
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class AlterarStatusRequestDto
{
    public bool EstaAtivo { get; set; }
}