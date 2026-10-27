using Ducks.Application.Services;
using Ducks.Domain.Entities;
using Ducks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Services;

public class EducacionalService : IEducacionalService
{
    private readonly DucksDbContext _context;

    public EducacionalService(DucksDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TrilhaAprendizagem>> ListarTrilhasAsync()
    {
        return await _context.TrilhasAprendizagem.ToListAsync();
    }

    public async Task<XPUsuario?> ObterXPUsuarioAsync(int usuarioID)
    {
        return await _context.XPUsuarios.FirstOrDefaultAsync(x => x.UsuarioID == usuarioID);
    }

    public async Task AdicionarXPAsync(int usuarioID, int quantidadeXP, string motivo)
    {
        var xpRegistro = await _context.XPUsuarios.FirstOrDefaultAsync(x => x.UsuarioID == usuarioID);
        
        if (xpRegistro == null)
        {
            xpRegistro = new XPUsuario
            {
                UsuarioID = usuarioID,
                TotalXP = quantidadeXP,
                NivelAtualID = 1,
                AtualizadoEm = DateTime.UtcNow
            };
            _context.XPUsuarios.Add(xpRegistro);
        }
        else
        {
            xpRegistro.TotalXP += quantidadeXP;
            xpRegistro.AtualizadoEm = DateTime.UtcNow;
        }

        _context.HistoricoXPs.Add(new HistoricoXP
        {
            UsuarioID = usuarioID,
            QuantidadeXP = quantidadeXP,
            Motivo = motivo,
            CriadoEm = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}