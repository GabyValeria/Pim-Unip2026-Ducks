using System.ComponentModel.DataAnnotations;

namespace Ducks.Application.DTOs;

public sealed class TrilhaRequestDto
{
    [Required, StringLength(150, MinimumLength = 3)] public string NomeTrilha { get; set; } = string.Empty;
    [StringLength(2000)] public string Descricao { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int MateriaID { get; set; }
}

public sealed class ModuloRequestDto
{
    [Required, StringLength(150, MinimumLength = 3)] public string Titulo { get; set; } = string.Empty;
    [StringLength(2000)] public string? Descricao { get; set; }
    /// <summary>Posição desejada. Se omitida, o módulo vai para o final da trilha.</summary>
    [Range(1, 10000)] public int? IndiceOrdem { get; set; }
}

public sealed class AulaRequestDto
{
    [Required, StringLength(150, MinimumLength = 3)] public string Titulo { get; set; } = string.Empty;
    [StringLength(500)] public string UrlConteudo { get; set; } = string.Empty;
    [StringLength(20000)] public string? Conteudo { get; set; }
    /// <summary>Posição desejada. Se omitida, a aula vai para o final do módulo.</summary>
    [Range(1, 10000)] public int? IndiceOrdem { get; set; }
}

/// <summary>Lista completa de IDs na nova ordem (o primeiro recebe IndiceOrdem 1).</summary>
public sealed class ReordenarDto
{
    [Required, MinLength(1)] public List<int> Ids { get; set; } = [];
}

public sealed class FiltroConteudoDto : ParametrosPaginacao
{
    /// <summary>Filtra pelo estado (RASCUNHO, PENDENTE, APROVADO, REJEITADO, REMOVIDO).</summary>
    public string? Status { get; set; }
    /// <summary>Busca no título.</summary>
    public string? Busca { get; set; }
}

public sealed record ModuloDto(
    int ModuloID, int TrilhaID, string Titulo, string? Descricao, int IndiceOrdem,
    string Status, int TotalAulas, DateTime AtualizadoEm);

public sealed record AulaDto(
    int AulaID, int ModuloID, string Titulo, string UrlConteudo, string? Conteudo,
    int IndiceOrdem, string Status, DateTime AtualizadoEm);

public sealed record ModuloDetalheDto(
    int ModuloID, int TrilhaID, string Titulo, string? Descricao, int IndiceOrdem,
    string Status, DateTime AtualizadoEm, IReadOnlyList<AulaDto> Aulas);

public sealed record TrilhaDetalheDto(
    int TrilhaID, string NomeTrilha, string Descricao, int MateriaID, string Status,
    DateTime AtualizadoEm, IReadOnlyList<ModuloDetalheDto> Modulos);
