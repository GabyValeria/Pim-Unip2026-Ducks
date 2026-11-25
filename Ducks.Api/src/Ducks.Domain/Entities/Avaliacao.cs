namespace Ducks.Domain.Entities;

/// <summary>Questão do banco de questões (tabela Questoes do banco do grupo).</summary>
public class Questao
{
    public int QuestaoID { get; set; }
    public int? MateriaID { get; set; }
    public string TextoQuestao { get; set; } = string.Empty;
    public string TipoQuestao { get; set; } = TiposQuestao.MultiplaEscolha;
    public int Peso { get; set; } = 1;
    public string NivelDificuldade { get; set; } = NiveisDificuldade.Medio;
    public string Status { get; set; } = StatusConteudo.Rascunho;
    public int? TrilhaID { get; set; }
    public int? ModuloID { get; set; }
    public int? AulaID { get; set; }
    public string? Explicacao { get; set; }
    public string? Assunto { get; set; }
    public string? Tags { get; set; }
    public int? CriadoPorUsuarioID { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

public class AlternativaQuestao
{
    public int AlternativaID { get; set; }
    public int QuestaoID { get; set; }
    public string TextoAlternativa { get; set; } = string.Empty;
    public bool EhCorreta { get; set; }
    public string? Explicacao { get; set; }
}

public static class TiposQuestao
{
    public const string MultiplaEscolha = "MULTIPLA_ESCOLHA";
    public const string VerdadeiroFalso = "VERDADEIRO_FALSO";
    public static readonly string[] Todos = [MultiplaEscolha, VerdadeiroFalso];
}

public static class NiveisDificuldade
{
    public const string Facil = "FACIL";
    public const string Medio = "MEDIO";
    public const string Dificil = "DIFICIL";
    public static readonly string[] Todos = [Facil, Medio, Dificil];
}

/// <summary>Simulado ou prova (tabela Provas do banco do grupo).</summary>
public class Prova
{
    public int ProvaID { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string TipoProva { get; set; } = TiposProva.Geral;
    public int? MateriaID { get; set; }
    public int TotalQuestoes { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public string? Descricao { get; set; }
    public int? TrilhaID { get; set; }
    public int? ModuloID { get; set; }
    public int? TempoLimiteMinutos { get; set; }
    public int? MaxTentativas { get; set; }
    public int NotaMinimaAprovacao { get; set; } = 60;
    public int RecompensaXP { get; set; } = 50;
    public string Status { get; set; } = StatusConteudo.Rascunho;
    public int? CriadoPorUsuarioID { get; set; }
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

public class ProvaQuestao
{
    public int ProvaID { get; set; }
    public int QuestaoID { get; set; }
    public int OrdemQuestao { get; set; }
}

/// <summary>Tentativa de um usuário em um simulado (tabela ProvasUsuario).</summary>
public class ProvaUsuario
{
    public int ProvaUsuarioID { get; set; }
    public int UsuarioID { get; set; }
    public int ProvaID { get; set; }
    public DateTime IniciadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? ConcluidoEm { get; set; }
    /// <summary>Quantidade de acertos (coluna já existente no banco do grupo).</summary>
    public int Pontuacao { get; set; }
    public string Status { get; set; } = StatusTentativa.EmAndamento;
    public int NumeroTentativa { get; set; } = 1;
    public DateTime? PrazoEm { get; set; }
    public int TotalQuestoes { get; set; }
    public decimal NotaPercentual { get; set; }
    public int XPGanho { get; set; }
}

/// <summary>Mesmos valores da restrição CK_StatusProvaUsuario do banco do grupo.</summary>
public static class StatusTentativa
{
    public const string EmAndamento = "EM_ANDAMENTO";
    /// <summary>Finalizada pelo estudante ou automaticamente quando o tempo limite acabou.</summary>
    public const string Finalizada = "CONCLUIDA";
}

/// <summary>Tipos aceitos pela restrição CK_TipoProva do banco do grupo.</summary>
public static class TiposProva
{
    /// <summary>Sem matéria definida (MateriaID nulo).</summary>
    public const string Geral = "GERAL";
    /// <summary>Ligada a uma matéria (MateriaID preenchido).</summary>
    public const string Especifica = "ESPECIFICA";
}

public class RespostaProvaUsuario
{
    public int RespostaUsuarioID { get; set; }
    public int ProvaUsuarioID { get; set; }
    public int QuestaoID { get; set; }
    public int AlternativaSelecionadaID { get; set; }
    public bool EhCorreta { get; set; }
    public DateTime RespondidoEm { get; set; } = DateTime.UtcNow;
}
