namespace Ducks.Domain.Entities;

/// <summary>Histórico de cada decisão do fluxo de revisão (envio, aprovação, rejeição, remoção).</summary>
public class RevisaoConteudo
{
    public int RevisaoID { get; set; }
    public string TipoConteudo { get; set; } = string.Empty;
    public int ConteudoID { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string StatusAnterior { get; set; } = string.Empty;
    public string StatusNovo { get; set; } = string.Empty;
    public string? Justificativa { get; set; }
    public int UsuarioID { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public static class AcaoRevisao
{
    public const string EnviadoParaRevisao = "ENVIADO_REVISAO";
    public const string Aprovado = "APROVADO";
    public const string Rejeitado = "REJEITADO";
    public const string Removido = "REMOVIDO";
}
