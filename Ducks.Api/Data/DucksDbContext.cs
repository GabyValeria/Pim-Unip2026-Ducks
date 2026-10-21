using Ducks.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Api.Data;

public class DucksDbContext : DbContext
{
    public DucksDbContext(DbContextOptions<DucksDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<TrilhaAprendizagem> TrilhasAprendizagem { get; set; }
    public DbSet<Questao> Questoes { get; set; }
    public DbSet<XPUsuario> XPUsuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Garante que o EF Core respeita estritamente o esquema existente do ducks-db sem migrações automáticas
    }
}