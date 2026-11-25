using Ducks.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ducks.Infrastructure.Persistence;

/// <summary>
/// Mapeamento das tabelas acrescentadas para módulos/aulas, revisão de conteúdo, matrículas,
/// banco de questões, simulados, tentativas, desafios e recuperação de senha.
/// O script Database/02_novas_tabelas.sql cria essas tabelas no SQL Server.
/// </summary>
public partial class DucksDbContext
{
    public DbSet<Modulo> Modulos => Set<Modulo>();
    public DbSet<RevisaoConteudo> RevisoesConteudo => Set<RevisaoConteudo>();
    public DbSet<UsuarioTrilha> UsuarioTrilhas => Set<UsuarioTrilha>();
    public DbSet<ProgressoUsuario> ProgressosUsuario => Set<ProgressoUsuario>();
    public DbSet<RecompensaConcedida> RecompensasConcedidas => Set<RecompensaConcedida>();
    public DbSet<Questao> Questoes => Set<Questao>();
    public DbSet<AlternativaQuestao> AlternativasQuestao => Set<AlternativaQuestao>();
    public DbSet<Prova> Provas => Set<Prova>();
    public DbSet<ProvaQuestao> ProvaQuestoes => Set<ProvaQuestao>();
    public DbSet<ProvaUsuario> ProvasUsuario => Set<ProvaUsuario>();
    public DbSet<RespostaProvaUsuario> RespostasProvasUsuario => Set<RespostaProvaUsuario>();
    public DbSet<ParticipacaoDesafio> ParticipacoesDesafio => Set<ParticipacaoDesafio>();
    public DbSet<TokenRecuperacaoSenha> TokensRecuperacaoSenha => Set<TokenRecuperacaoSenha>();

    private static void ConfigurarTabelasNovas(ModelBuilder modelBuilder)
    {
        // Colunas acrescentadas em tabelas que já existiam.
        modelBuilder.Entity<Aula>(entity =>
        {
            entity.Property(e => e.Conteudo).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => new { e.ModuloID, e.IndiceOrdem });
        });
        modelBuilder.Entity<Desafio>(entity =>
        {
            entity.Property(e => e.TipoRequisito).IsRequired().HasMaxLength(30);
        });

        modelBuilder.Entity<Modulo>(entity =>
        {
            entity.ToTable("Modulos");
            entity.HasKey(e => e.ModuloID);
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Descricao).HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
            entity.HasIndex(e => new { e.TrilhaID, e.IndiceOrdem });
        });

        modelBuilder.Entity<RevisaoConteudo>(entity =>
        {
            entity.ToTable("RevisoesConteudo");
            entity.HasKey(e => e.RevisaoID);
            entity.Property(e => e.TipoConteudo).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Acao).IsRequired().HasMaxLength(30);
            entity.Property(e => e.StatusAnterior).IsRequired().HasMaxLength(30);
            entity.Property(e => e.StatusNovo).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Justificativa).HasMaxLength(1000);
            entity.HasIndex(e => new { e.TipoConteudo, e.ConteudoID, e.CriadoEm });
        });

        modelBuilder.Entity<UsuarioTrilha>(entity =>
        {
            entity.ToTable("UsuarioTrilhas");
            entity.HasKey(e => e.UsuarioTrilhaID);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
            entity.Property(e => e.ProgressoPercentual).HasPrecision(5, 2);
            entity.HasIndex(e => new { e.UsuarioID, e.TrilhaID }).IsUnique();
        });

        modelBuilder.Entity<ProgressoUsuario>(entity =>
        {
            entity.ToTable("ProgressoUsuario");
            entity.HasKey(e => e.ProgressoID);
            entity.HasIndex(e => new { e.UsuarioID, e.AulaID }).IsUnique();
        });

        modelBuilder.Entity<RecompensaConcedida>(entity =>
        {
            entity.ToTable("RecompensasConcedidas");
            entity.HasKey(e => e.RecompensaID);
            entity.Property(e => e.TipoOrigem).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => new { e.UsuarioID, e.TipoOrigem, e.OrigemID }).IsUnique();
        });

        modelBuilder.Entity<Questao>(entity =>
        {
            entity.ToTable("Questoes");
            entity.HasKey(e => e.QuestaoID);
            entity.Property(e => e.TextoQuestao).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.TipoQuestao).IsRequired().HasMaxLength(50);
            entity.Property(e => e.NivelDificuldade).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Explicacao).HasMaxLength(2000);
            entity.Property(e => e.Assunto).HasMaxLength(150);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.HasIndex(e => new { e.Status, e.NivelDificuldade });
            entity.HasIndex(e => e.TrilhaID);
            entity.HasIndex(e => e.ModuloID);
        });

        modelBuilder.Entity<AlternativaQuestao>(entity =>
        {
            entity.ToTable("AlternativasQuestao");
            entity.HasKey(e => e.AlternativaID);
            entity.Property(e => e.TextoAlternativa).IsRequired().HasColumnType("nvarchar(max)");
            entity.Property(e => e.Explicacao).HasMaxLength(1000);
            entity.HasIndex(e => e.QuestaoID);
        });

        modelBuilder.Entity<Prova>(entity =>
        {
            entity.ToTable("Provas");
            entity.HasKey(e => e.ProvaID);
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(150);
            entity.Property(e => e.TipoProva).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Descricao).HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
            entity.HasIndex(e => new { e.Status, e.TrilhaID });
        });

        modelBuilder.Entity<ProvaQuestao>(entity =>
        {
            entity.ToTable("ProvaQuestoes");
            entity.HasKey(e => new { e.ProvaID, e.QuestaoID });
            entity.HasIndex(e => e.QuestaoID);
        });

        modelBuilder.Entity<ProvaUsuario>(entity =>
        {
            entity.ToTable("ProvasUsuario");
            entity.HasKey(e => e.ProvaUsuarioID);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
            entity.Property(e => e.NotaPercentual).HasPrecision(5, 2);
            entity.HasIndex(e => new { e.UsuarioID, e.ProvaID, e.Status });
        });

        modelBuilder.Entity<RespostaProvaUsuario>(entity =>
        {
            entity.ToTable("RespostasProvasUsuario");
            entity.HasKey(e => e.RespostaUsuarioID);
            entity.HasIndex(e => new { e.ProvaUsuarioID, e.QuestaoID }).IsUnique();
        });

        modelBuilder.Entity<ParticipacaoDesafio>(entity =>
        {
            entity.ToTable("ParticipacoesDesafio");
            entity.HasKey(e => e.ParticipacaoID);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
            entity.HasIndex(e => new { e.DesafioID, e.UsuarioID }).IsUnique();
            entity.HasIndex(e => e.UsuarioID);
        });

        modelBuilder.Entity<TokenRecuperacaoSenha>(entity =>
        {
            entity.ToTable("TokensRecuperacaoSenha");
            entity.HasKey(e => e.TokenID);
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(64).HasColumnType("char(64)");
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.UsuarioID, e.CriadoEm });
        });
    }
}
