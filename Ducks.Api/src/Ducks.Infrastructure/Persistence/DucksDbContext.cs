using Ducks.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Persistence;

public partial class DucksDbContext(DbContextOptions<DucksDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TrilhaAprendizagem> TrilhasAprendizagem => Set<TrilhaAprendizagem>();
    public DbSet<Aula> Aulas => Set<Aula>();
    public DbSet<XPUsuario> XPUsuarios => Set<XPUsuario>();
    public DbSet<HistoricoXP> HistoricoXPs => Set<HistoricoXP>();
    public DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Conquista> Conquistas => Set<Conquista>();
    public DbSet<Desafio> Desafios => Set<Desafio>();
    public DbSet<ConfiguracaoSistema> ConfiguracoesSistema => Set<ConfiguracaoSistema>();
    public DbSet<UsuarioConquista> UsuariosConquistas => Set<UsuarioConquista>();
    // Tabelas novas (módulos, revisão, matrículas, avaliações, desafios e recuperação de senha)
    // estão em DucksDbContext.Novos.cs.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var isInMemoryProvider = Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true;
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
            if (isInMemoryProvider)
                entity.Property(e => e.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            else
                entity.Property(e => e.RowVersion).IsRowVersion();
            entity.HasCheckConstraint("CK_XPUsuario_TotalXP_NaoNegativo", "TotalXP >= 0");
        });
        modelBuilder.Entity<HistoricoXP>(entity =>
        {
            entity.ToTable("HistoricoXP");
            entity.HasKey(e => e.HistoricoXPID);
            entity.Property(e => e.Motivo).IsRequired().HasMaxLength(250);
        });
        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.ToTable("ChatConversations");
            entity.HasKey(e => e.ConversationId);
            entity.HasAlternateKey(e => new { e.ConversationId, e.UsuarioID });
            entity.HasIndex(e => new { e.UsuarioID, e.AtualizadoEm });
        });
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessages");
            entity.HasKey(e => e.MessageId);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Content).IsRequired().HasColumnType("nvarchar(max)");
            entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
            entity.HasOne<ChatConversation>()
                .WithMany()
                .HasForeignKey(e => new { e.ConversationId, e.UsuarioID })
                .HasPrincipalKey(c => new { c.ConversationId, c.UsuarioID })
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Conquista>(entity =>
        {
            entity.ToTable("Conquistas");
            entity.HasKey(e => e.ConquistaID);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Descricao).HasMaxLength(1000);
            entity.HasCheckConstraint("CK_Conquistas_XPNecessario", "XPNecessario >= 0");
        });
        modelBuilder.Entity<Desafio>(entity =>
        {
            entity.ToTable("Desafios");
            entity.HasKey(e => e.DesafioID);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Descricao).HasMaxLength(1000);
            entity.HasCheckConstraint("CK_Desafios_RecompensaXP", "RecompensaXP >= 0");
        });
        modelBuilder.Entity<UsuarioConquista>(entity =>
        {
            entity.ToTable("UsuarioConquistas");
            entity.HasKey(e => new { e.UsuarioID, e.ConquistaID });
            entity.HasIndex(e => e.ConquistaID);
        });
        modelBuilder.Entity<ConfiguracaoSistema>(entity =>
        {
            entity.ToTable("ConfiguracoesSistema");
            entity.HasKey(e => e.Chave);
            entity.Property(e => e.Chave).HasMaxLength(100);
            entity.Property(e => e.Valor).IsRequired().HasMaxLength(4000);
        });
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(64).HasColumnType("char(64)");
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.UsuarioID, e.TokenFamilyId });
            entity.Property(e => e.Dispositivo).HasMaxLength(150);
            if (isInMemoryProvider)
                entity.Property(e => e.RowVersion).IsConcurrencyToken().ValueGeneratedNever();
            else
                entity.Property(e => e.RowVersion).IsRowVersion();
        });

        ConfigurarTabelasNovas(modelBuilder);
    }
}
