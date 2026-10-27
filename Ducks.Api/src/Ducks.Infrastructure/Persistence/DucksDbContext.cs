using Ducks.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Persistence;

public class DucksDbContext : DbContext
{
    public DucksDbContext(DbContextOptions<DucksDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TrilhaAprendizagem> TrilhasAprendizagem => Set<TrilhaAprendizagem>();
    public DbSet<Aula> Aulas => Set<Aula>();
    public DbSet<XPUsuario> XPUsuarios => Set<XPUsuario>();
    public DbSet<HistoricoXP> HistoricoXPs => Set<HistoricoXP>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(e => e.UsuarioID);
            entity.Property(e => e.NomeUsuario).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.Property(e => e.SenhaHash).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<TrilhaAprendizagem>(entity =>
        {
            entity.ToTable("TrilhasAprendizagem");
            entity.HasKey(e => e.TrilhaID);
            entity.Property(e => e.NomeTrilha).IsRequired().HasMaxLength(150);
        });

        modelBuilder.Entity<Aula>(entity =>
        {
            entity.ToTable("Aulas");
            entity.HasKey(e => e.AulaID);
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(150);
        });

        modelBuilder.Entity<XPUsuario>(entity =>
        {
            entity.ToTable("XPUsuario");
            entity.HasKey(e => e.UsuarioID);
        });

        modelBuilder.Entity<HistoricoXP>(entity =>
        {
            entity.ToTable("HistoricoXP");
            entity.HasKey(e => e.HistoricoXPID);
        });
    }
}