using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Domain.Exceptions;
using Ducks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Services;

public class EducacionalService(DucksDbContext context) : IEducacionalService
{
    public async Task<IEnumerable<TrilhaAprendizagem>> ListarTrilhasAsync(CancellationToken cancellationToken = default)
    {
        return await context.TrilhasAprendizagem
            .AsNoTracking()
            .Where(t => t.Status == "APROVADO")
            .OrderBy(t => t.NomeTrilha)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public Task<XPUsuario?> ObterXPUsuarioAsync(int usuarioID, CancellationToken cancellationToken = default)
    {
        return context.XPUsuarios.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UsuarioID == usuarioID, cancellationToken);
    }

    // Esta operação é reservada à administração. Recompensas de aluno devem ser calculadas
    // pelo servidor a partir de eventos de conclusão de atividade, nunca de valores enviados pelo cliente.
    public async Task AdicionarXPAsync(int usuarioID, int quantidadeXP, string motivo, CancellationToken cancellationToken = default)
    {
        if (usuarioID <= 0) throw new BusinessRuleException("Usuário inválido.");
        if (quantidadeXP == 0 || quantidadeXP < -10000 || quantidadeXP > 10000)
            throw new BusinessRuleException("A quantidade de XP deve estar entre -10000 e 10000 e não pode ser zero.");
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 250)
            throw new BusinessRuleException("Informe um motivo com até 250 caracteres.");

        var xp = await context.XPUsuarios.FirstOrDefaultAsync(x => x.UsuarioID == usuarioID, cancellationToken);
        if (xp is null)
        {
            if (quantidadeXP < 0) throw new BusinessRuleException("Não é possível criar saldo de XP negativo.");
            xp = new XPUsuario
            {
                UsuarioID = usuarioID,
                TotalXP = quantidadeXP,
                NivelAtualID = 1,
                AtualizadoEm = DateTime.UtcNow
            };
            context.XPUsuarios.Add(xp);
        }
        else
        {
            var novoTotal = checked(xp.TotalXP + quantidadeXP);
            if (novoTotal < 0) throw new BusinessRuleException("O saldo de XP não pode ficar negativo.");
            xp.TotalXP = novoTotal;
            xp.AtualizadoEm = DateTime.UtcNow;
        }

        context.HistoricoXPs.Add(new HistoricoXP
        {
            UsuarioID = usuarioID,
            QuantidadeXP = quantidadeXP,
            Motivo = motivo.Trim(),
            CriadoEm = DateTime.UtcNow
        });

        // Concede automaticamente conquistas ativas cujo marco de XP foi atingido.
        var totalAtual = xp.TotalXP;
        var conquistasElegiveis = await context.Conquistas.AsNoTracking()
            .Where(c => c.Ativa && c.XPNecessario <= totalAtual)
            .Select(c => c.ConquistaID).ToListAsync(cancellationToken);
        if (conquistasElegiveis.Count > 0)
        {
            var jaConquistadas = await context.UsuariosConquistas.AsNoTracking()
                .Where(uc => uc.UsuarioID == usuarioID && conquistasElegiveis.Contains(uc.ConquistaID))
                .Select(uc => uc.ConquistaID).ToListAsync(cancellationToken);
            foreach (var conquistaId in conquistasElegiveis.Except(jaConquistadas))
                context.UsuariosConquistas.Add(new UsuarioConquista { UsuarioID = usuarioID, ConquistaID = conquistaId, ConquistadaEm = DateTime.UtcNow });
        }

        await context.SaveChangesAsync(cancellationToken);

    }
}
