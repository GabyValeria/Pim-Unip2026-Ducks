using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Threading.Channels;
using Ducks.Application.DTOs;
using Ducks.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ducks.Infrastructure.Services.Comum;

namespace Ducks.Infrastructure.Integrations.Email;

/// <summary>
/// Envio por SMTP usando System.Net.Mail (já faz parte do .NET; nenhuma biblioteca nova).
/// Configuração só por variáveis de ambiente / User Secrets:
/// Email__Provedor=Smtp, Email__Remetente, Email__Smtp__Host, Email__Smtp__Porta, Email__Smtp__Usuario,
/// Email__Smtp__Senha, Email__Smtp__UsarSsl.
/// </summary>
public sealed class SmtpEmailService(IConfiguration configuration) : IEmailService
{
    public async Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct = default)
    {
        var host = configuration["Email:Smtp:Host"] ?? throw new InvalidOperationException("Email:Smtp:Host não configurado.");
        var remetente = configuration["Email:Remetente"] ?? throw new InvalidOperationException("Email:Remetente não configurado.");
        using var cliente = new SmtpClient(host, configuration.LerInt("Email:Smtp:Porta", 587))
        {
            EnableSsl = configuration.LerBool("Email:Smtp:UsarSsl", true),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        var usuario = configuration["Email:Smtp:Usuario"];
        if (!string.IsNullOrWhiteSpace(usuario))
            cliente.Credentials = new NetworkCredential(usuario, configuration["Email:Smtp:Senha"]);
        using var email = new MailMessage(new MailAddress(remetente, configuration["Email:NomeRemetente"] ?? "Ducks"), new MailAddress(mensagem.Para))
        {
            Subject = mensagem.Assunto,
            Body = mensagem.CorpoHtml ?? mensagem.CorpoTexto,
            IsBodyHtml = mensagem.CorpoHtml is not null
        };
        await cliente.SendMailAsync(email, ct);
    }
}

/// <summary>
/// Implementação de desenvolvimento: não envia nada, só registra no log e guarda as últimas mensagens
/// (os testes leem daqui). Em Development o link aparece no log para facilitar o teste manual.
/// </summary>
public sealed class MockEmailService(ILogger<MockEmailService> logger, IHostEnvironment environment) : IEmailService
{
    private readonly ConcurrentQueue<MensagemEmail> _enviadas = new();
    public IReadOnlyCollection<MensagemEmail> Enviadas => _enviadas;

    public Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct = default)
    {
        _enviadas.Enqueue(mensagem);
        while (_enviadas.Count > 100) _enviadas.TryDequeue(out _);
        if (environment.IsDevelopment())
            logger.LogInformation("[E-mail simulado] Para: {Para} | Assunto: {Assunto}\n{Corpo}", mensagem.Para, mensagem.Assunto, mensagem.CorpoTexto);
        else
            logger.LogWarning("E-mail não configurado (Email__Provedor). Mensagem para {Para} não foi enviada.", mensagem.Para);
        return Task.CompletedTask;
    }
}

public sealed class FilaEmail : IFilaEmail
{
    private readonly Channel<MensagemEmail> _canal = Channel.CreateBounded<MensagemEmail>(new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropOldest });
    public ChannelReader<MensagemEmail> Leitor => _canal.Reader;
    public void Enfileirar(MensagemEmail mensagem) => _canal.Writer.TryWrite(mensagem);
}

/// <summary>
/// Envia os e-mails da fila em segundo plano. Assim "esqueci minha senha" responde no mesmo tempo
/// exista ou não o e-mail (não dá para descobrir cadastros pelo tempo de resposta).
/// </summary>
public sealed class EnvioEmailBackgroundService(FilaEmail fila, IEmailService email, ILogger<EnvioEmailBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var mensagem in fila.Leitor.ReadAllAsync(stoppingToken))
        {
            try { await email.EnviarAsync(mensagem, stoppingToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falha ao enviar e-mail para {Para}.", mensagem.Para);
            }
        }
    }
}
