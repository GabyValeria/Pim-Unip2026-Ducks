using System.ComponentModel.DataAnnotations;

namespace Ducks.Application.DTOs;

public sealed class EsqueciSenhaDto
{
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
}

public sealed class RedefinirSenhaDto
{
    [Required, StringLength(200, MinimumLength = 20)] public string Token { get; set; } = string.Empty;
    /// <summary>Mesma regra do cadastro: 8 a 128 caracteres.</summary>
    [Required, StringLength(128, MinimumLength = 8)] public string NovaSenha { get; set; } = string.Empty;
}

public sealed record MensagemEmail(string Para, string Assunto, string CorpoTexto, string? CorpoHtml = null);
