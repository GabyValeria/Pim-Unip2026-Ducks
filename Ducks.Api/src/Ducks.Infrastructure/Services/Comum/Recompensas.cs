using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Services.Comum;

/// <summary>
/// Concede XP e medalhas reaproveitando o EducacionalService.AdicionarXPAsync já existente
/// (que também libera as conquistas por marco de XP). Cada evento (aula, trilha, prova, desafio)
/// só é pago uma vez: a tabela RecompensasConcedidas tem chave única por usuário + origem.
/// </summary>
internal sealed class Recompensas(DucksDbContext context, IEducacionalService educacional)
{
    public const string ChaveXPAula = "XP_CONCLUSAO_AULA";
    public const string ChaveXPTrilha = "XP_CONCLUSAO_TRILHA";

    public Task<bool> JaConcedidaAsync(int usuarioId, string tipoOrigem, int origemId, CancellationToken ct) =>
        context.RecompensasConcedidas.AnyAsync(r => r.UsuarioID == usuarioId && r.TipoOrigem == tipoOrigem && r.OrigemID == origemId, ct);

    /// <summary>Devolve o XP efetivamente concedido (0 se o evento já tinha sido pago).</summary>
    public async Task<int> ConcederAsync(int usuarioId, string tipoOrigem, int origemId, int xp, int? conquistaId, string motivo, CancellationToken ct)
    {
        if (await JaConcedidaAsync(usuarioId, tipoOrigem, origemId, ct)) return 0;
        xp = Math.Clamp(xp, 0, 10000);
        context.RecompensasConcedidas.Add(new RecompensaConcedida
        {
            UsuarioID = usuarioId, TipoOrigem = tipoOrigem, OrigemID = origemId, QuantidadeXP = xp,
            ConquistaID = conquistaId, CriadoEm = DateTime.UtcNow
        });
        if (conquistaId.HasValue
            && await context.Conquistas.AnyAsync(c => c.ConquistaID == conquistaId.Value, ct)
            && !await context.UsuariosConquistas.AnyAsync(u => u.UsuarioID == usuarioId && u.ConquistaID == conquistaId.Value, ct))
            context.UsuariosConquistas.Add(new UsuarioConquista { UsuarioID = usuarioId, ConquistaID = conquistaId.Value, ConquistadaEm = DateTime.UtcNow });

        // AdicionarXPAsync grava tudo (inclusive o registro acima) no mesmo SaveChanges.
        if (xp > 0) await educacional.AdicionarXPAsync(usuarioId, xp, motivo.Length > 100 ? motivo[..100] : motivo, ct);
        else await context.SaveChangesAsync(ct);
        return xp;
    }

    /// <summary>Lê um parâmetro numérico da tela de configurações do Admin (ConfiguracoesSistema).</summary>
    public async Task<int> LerConfiguracaoAsync(string chave, int padrao, CancellationToken ct)
    {
        var valor = await context.ConfiguracoesSistema.AsNoTracking().Where(c => c.Chave == chave).Select(c => c.Valor).FirstOrDefaultAsync(ct);
        return int.TryParse(valor, out var numero) && numero >= 0 ? numero : padrao;
    }
}
