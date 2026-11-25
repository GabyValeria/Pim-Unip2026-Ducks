using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ducks.Api.Controllers;

/// <summary>Esqueci minha senha / redefinir senha. As duas rotas têm limite de requisições por IP.</summary>
[AllowAnonymous]
[ApiController]
[Route("api/auth")]
[EnableRateLimiting("RecuperacaoSenhaPerIp")]
public sealed class RecuperacaoSenhaController(IRecuperacaoSenhaService recuperacao) : ControllerBase
{
    /// <summary>Sempre responde 202 com a mesma mensagem, exista ou não o e-mail.</summary>
    [HttpPost("esqueci-senha")]
    public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaDto dto, CancellationToken ct)
    {
        await recuperacao.SolicitarAsync(dto.Email, ct);
        Response.Headers["Cache-Control"] = "no-store";
        return Accepted(new { mensagem = "Se o e-mail estiver cadastrado, você receberá as instruções para redefinir a senha." });
    }

    /// <summary>Troca a senha com o token recebido por e-mail. Encerra todas as sessões anteriores.</summary>
    [HttpPost("redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaDto dto, CancellationToken ct)
    {
        await recuperacao.RedefinirAsync(dto.Token, dto.NovaSenha, ct);
        return NoContent();
    }
}
