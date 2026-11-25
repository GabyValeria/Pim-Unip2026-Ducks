namespace Ducks.Domain.Entities;

public sealed class Conquista
{
    public int ConquistaID { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int XPNecessario { get; set; }
    public bool Ativa { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public sealed class Desafio
{
    public int DesafioID { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int RecompensaXP { get; set; }
    public DateTime? InicioEm { get; set; }
    public DateTime? FimEm { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // Acrescentados na participação de estudantes em desafios (todos opcionais).
    public string TipoRequisito { get; set; } = TiposRequisitoDesafio.Nenhum;
    public int MetaRequisito { get; set; }
    public int? TrilhaID { get; set; }
    public int? ConquistaID { get; set; }
}

public sealed class ConfiguracaoSistema
{
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    public int? AtualizadoPor { get; set; }
}


public sealed class UsuarioConquista
{
    public int UsuarioID { get; set; }
    public int ConquistaID { get; set; }
    public DateTime ConquistadaEm { get; set; } = DateTime.UtcNow;
}
