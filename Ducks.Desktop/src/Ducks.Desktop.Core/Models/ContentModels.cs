using System;
using System.Collections.Generic;

namespace Ducks.Desktop.Core.Models
{
    // ------------------------------------------------------------------ Estrutura (módulos e aulas)
    // Contratos: Ducks.Application.DTOs.ConteudoDtos

    public class ModuloDto
    {
        public int ModuloID { get; set; }
        public int TrilhaID { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int IndiceOrdem { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalAulas { get; set; }
        public DateTime AtualizadoEm { get; set; }
    }

    public class AulaDto
    {
        public int AulaID { get; set; }
        public int ModuloID { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string UrlConteudo { get; set; } = string.Empty;
        public string? Conteudo { get; set; }
        public int IndiceOrdem { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime AtualizadoEm { get; set; }
    }

    public class ModuloRequestDto
    {
        public string Titulo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int? IndiceOrdem { get; set; }
    }

    public class AulaRequestDto
    {
        public string Titulo { get; set; } = string.Empty;
        public string UrlConteudo { get; set; } = string.Empty;
        public string? Conteudo { get; set; }
        public int? IndiceOrdem { get; set; }
    }

    /// <summary>Lista completa de IDs na nova ordem (o primeiro recebe IndiceOrdem 1).</summary>
    public class ReordenarRequestDto
    {
        public List<int> Ids { get; set; } = new();
    }

    // ------------------------------------------------------------------ Avaliações (questões e simulados)
    // Contratos: Ducks.Application.DTOs.AvaliacaoDtos

    public class AlternativaDto
    {
        public int AlternativaID { get; set; }
        public string Texto { get; set; } = string.Empty;
        public bool Correta { get; set; }
        public string? Explicacao { get; set; }
    }

    public class AlternativaRequestDto
    {
        public int? AlternativaID { get; set; }
        public string Texto { get; set; } = string.Empty;
        public bool Correta { get; set; }
        public string? Explicacao { get; set; }
    }

    public class QuestaoDto
    {
        public int QuestaoID { get; set; }
        public string Enunciado { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Dificuldade { get; set; } = string.Empty;
        public int Peso { get; set; }
        public string? Explicacao { get; set; }
        public string? Assunto { get; set; }
        public List<string> Tags { get; set; } = new();
        public int? TrilhaID { get; set; }
        public int? ModuloID { get; set; }
        public int? AulaID { get; set; }
        public int? MateriaID { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; }
        public DateTime AtualizadoEm { get; set; }
        public List<AlternativaDto> Alternativas { get; set; } = new();
    }

    public class QuestaoRequestDto
    {
        public string Enunciado { get; set; } = string.Empty;
        public string Tipo { get; set; } = "MULTIPLA_ESCOLHA";
        public string Dificuldade { get; set; } = "MEDIO";
        public int Peso { get; set; } = 1;
        public string? Explicacao { get; set; }
        public string? Assunto { get; set; }
        public List<string> Tags { get; set; } = new();
        public int? TrilhaID { get; set; }
        public int? ModuloID { get; set; }
        public int? AulaID { get; set; }
        public int? MateriaID { get; set; }
        public List<AlternativaRequestDto> Alternativas { get; set; } = new();
    }

    public class QuestaoDoSimuladoDto
    {
        public int QuestaoID { get; set; }
        public int Ordem { get; set; }
        public string Enunciado { get; set; } = string.Empty;
        public string Dificuldade { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class SimuladoDto
    {
        public int ProvaID { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string TipoProva { get; set; } = string.Empty;
        public int? TrilhaID { get; set; }
        public int? ModuloID { get; set; }
        public int? MateriaID { get; set; }
        public int? TempoLimiteMinutos { get; set; }
        public int? MaxTentativas { get; set; }
        public int NotaMinimaAprovacao { get; set; }
        public int RecompensaXP { get; set; }
        public int NumeroQuestoes { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalTentativas { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime AtualizadoEm { get; set; }
        public List<QuestaoDoSimuladoDto> Questoes { get; set; } = new();
    }

    public class SimuladoRequestDto
    {
        public string Titulo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int? TrilhaID { get; set; }
        public int? ModuloID { get; set; }
        public int? TempoLimiteMinutos { get; set; }
        public int? MaxTentativas { get; set; }
        public int NotaMinimaAprovacao { get; set; } = 60;
        public int RecompensaXP { get; set; } = 50;
        public int NumeroQuestoes { get; set; } = 10;
    }

    public class DefinirQuestoesRequestDto
    {
        public List<int> QuestaoIds { get; set; } = new();
    }

    public class SortearQuestoesRequestDto
    {
        public int? Quantidade { get; set; }
        public string? Dificuldade { get; set; }
        public string? Tag { get; set; }
        public string? Assunto { get; set; }
        public int? TrilhaID { get; set; }
        public int? ModuloID { get; set; }
        public bool SubstituirAtuais { get; set; } = true;
    }

    // ------------------------------------------------------------------ XP
    // Contrato: AdicionarXpDto (EducacionalController): UsuarioID >= 1, QuantidadeXP -10000..10000, Motivo 3..250.

    public class AdicionarXpRequestDto
    {
        public int UsuarioID { get; set; }
        public int QuantidadeXP { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
