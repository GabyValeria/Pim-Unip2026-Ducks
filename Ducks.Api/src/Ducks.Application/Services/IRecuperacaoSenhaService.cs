using Ducks.Application.DTOs;

namespace Ducks.Application.Services;

public interface IRecuperacaoSenhaService
{
    /// <summary>Gera o token e coloca o e-mail na fila. Não informa se o e-mail existe.</summary>
    Task SolicitarAsync(string email, CancellationToken ct = default);
    /// <summary>Troca a senha com um token válido e encerra as sessões anteriores.</summary>
    Task RedefinirAsync(string token, string novaSenha, CancellationToken ct = default);
}

/// <summary>Envio de e-mail. Implementações: SMTP (produção) e Mock (desenvolvimento/testes).</summary>
public interface IEmailService
{
    Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct = default);
}

/// <summary>Fila em memória: a requisição só enfileira, e o envio acontece em segundo plano.</summary>
public interface IFilaEmail
{
    void Enfileirar(MensagemEmail mensagem);
}
