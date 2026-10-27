using Ducks.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Persistence;

public class DucksDbContext : DbContext
{
    public DucksDbContext(DbContextOptions<DucksDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(e => e.UsuarioID);
            
            entity.Property(e => e.NomeUsuario)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.HashSenha)
                .IsRequired();

            entity.Property(e => e.SaltSenha)
                .IsRequired();

            entity.HasIndex(e => e.Email)
                .IsUnique();
        });
    }
}