-- ============================================================================
-- PROJETO DUCKS - BANCO DE DADOS [ducks-db]
-- Arquivo: tabelas.sql
-- Descrição: Criação das 24 tabelas do esquema [dbo], suas Primary Keys,
--            Unique Constraints e valores padrão (Default Constraints).
-- Ordem: Executado após infraestrutura.sql e antes de restricoes_e_indices.sql.
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- 1. SEGURANÇA, USUÁRIOS E CONFIGURAÇÕES DO SISTEMA
-- ============================================================================

CREATE TABLE [dbo].[Usuarios] (
    [UsuarioID] [int] IDENTITY(1,1) NOT NULL,
    [NomeUsuario] [nvarchar](100) NOT NULL,
    [Email] [nvarchar](255) NOT NULL,
    [EstaAtivo] [bit] NOT NULL CONSTRAINT [DF_Usuarios_EstaAtivo] DEFAULT ((1)),
    [CriadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_Usuarios_CriadoEm] DEFAULT (sysutcdatetime()),
    [AtualizadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_Usuarios_AtualizadoEm] DEFAULT (sysutcdatetime()),
    [SenhaHash] [nvarchar](255) NOT NULL CONSTRAINT [DF_Usuarios_SenhaHash] DEFAULT (''),
    [SenhaAlteradaEm] [datetime2](7) NULL,
    CONSTRAINT [PK_Usuarios] PRIMARY KEY CLUSTERED ([UsuarioID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UQ_Usuarios_NomeUsuario] UNIQUE NONCLUSTERED ([NomeUsuario] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UQ_Usuarios_Email] UNIQUE NONCLUSTERED ([Email] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[Papeis] (
    [PapelID] [int] IDENTITY(1,1) NOT NULL,
    [NomePapel] [nvarchar](50) NOT NULL,
    [Descricao] [nvarchar](255) NULL,
    [CriadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_Papeis_CriadoEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_Papeis] PRIMARY KEY CLUSTERED ([PapelID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UQ_Papeis_NomePapel] UNIQUE NONCLUSTERED ([NomePapel] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[UsuarioPapeis] (
    [UsuarioID] [int] NOT NULL,
    [PapelID] [int] NOT NULL,
    [AtribuidoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_UsuarioPapeis_AtribuidoEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_UsuarioPapeis] PRIMARY KEY CLUSTERED ([UsuarioID] ASC, [PapelID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[RefreshTokens] (
    [Id] [uniqueidentifier] NOT NULL,
    [UsuarioID] [int] NOT NULL,
    [TokenHash] [char](64) NOT NULL,
    [TokenFamilyId] [uniqueidentifier] NOT NULL,
    [CriadoEm] [datetime2](7) NOT NULL,
    [ExpiraEm] [datetime2](7) NOT NULL,
    [RevogadoEm] [datetime2](7) NULL,
    [SubstituidoPorId] [uniqueidentifier] NULL,
    [Dispositivo] [nvarchar](150) NULL,
    [RowVersion] [timestamp] NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([Id] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[TokensRecuperacaoSenha] (
    [TokenID] [int] IDENTITY(1,1) NOT NULL,
    [UsuarioID] [int] NOT NULL,
    [TokenHash] [char](64) NOT NULL,
    [CriadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_TokensRecuperacaoSenha_CriadoEm] DEFAULT (sysutcdatetime()),
    [ExpiraEm] [datetime2](7) NOT NULL,
    [UsadoEm] [datetime2](7) NULL,
    CONSTRAINT [PK_TokensRecuperacaoSenha] PRIMARY KEY CLUSTERED ([TokenID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[LogsAuditoria] (
    [LogID] [int] IDENTITY(1,1) NOT NULL,
    [UsuarioID] [int] NULL,
    [Acao] [nvarchar](100) NOT NULL,
    [Detalhes] [nvarchar](max) NULL,
    [EnderecoIP] [nvarchar](50) NULL,
    [CriadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_LogsAuditoria_CriadoEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_LogsAuditoria] PRIMARY KEY CLUSTERED ([LogID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO

CREATE TABLE [dbo].[ConfiguracoesPlataforma] (
    [ConfiguracaoID] [int] IDENTITY(1,1) NOT NULL,
    [Chave] [nvarchar](100) NOT NULL,
    [Valor] [nvarchar](max) NOT NULL,
    [Descricao] [nvarchar](255) NULL,
    [AtualizadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_Config_AtualizadoEm] DEFAULT (sysutcdatetime()),
    [AtualizadoPorUsuarioID] [int] NULL,
    CONSTRAINT [PK_ConfiguracoesPlataforma] PRIMARY KEY CLUSTERED ([ConfiguracaoID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UQ_ConfiguracoesPlataforma_Chave] UNIQUE NONCLUSTERED ([Chave] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO

CREATE TABLE [dbo].[ConfiguracoesSistema] (
    [Chave] [nvarchar](100) NOT NULL,
    [Valor] [nvarchar](4000) NOT NULL,
    [AtualizadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_ConfiguracoesSistema_AtualizadoEm] DEFAULT (sysutcdatetime()),
    [AtualizadoPor] [int] NULL,
    CONSTRAINT [PK_ConfiguracoesSistema] PRIMARY KEY CLUSTERED ([Chave] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

-- ============================================================================
-- 2. DOMÍNIO PEDAGÓGICO (MATÉRIAS, TRILHAS, MÓDULOS, AULAS E CURADORIA)
-- ============================================================================

CREATE TABLE [dbo].[Materias] (
    [MateriaID] [int] IDENTITY(1,1) NOT NULL,
    [NomeMateria] [nvarchar](100) NOT NULL,
    [Descricao] [nvarchar](500) NULL,
    CONSTRAINT [PK_Materias] PRIMARY KEY CLUSTERED ([MateriaID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UQ_Materias_NomeMateria] UNIQUE NONCLUSTERED ([NomeMateria] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[TrilhasAprendizagem] (
    [TrilhaID] [int] IDENTITY(1,1) NOT NULL,
    [NomeTrilha] [nvarchar](150) NOT NULL,
    [Descricao] [nvarchar](max) NULL,
    [MateriaID] [int] NOT NULL,
    [Status] [nvarchar](30) NOT NULL CONSTRAINT [DF_Trilhas_Status] DEFAULT ('PENDENTE'),
    [CriadoPorUsuarioID] [int] NULL,
    [AprovadoPorUsuarioID] [int] NULL,
    [AtualizadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_Trilhas_AtualizadoEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_TrilhasAprendizagem] PRIMARY KEY CLUSTERED ([TrilhaID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO

CREATE TABLE [dbo].[Modulos] (
    [ModuloID] [int] IDENTITY(1,1) NOT NULL,
    [TrilhaID] [int] NOT NULL,
    [Titulo] [nvarchar](150) NOT NULL,
    [IndiceOrdem] [int] NOT NULL,
    [Descricao] [nvarchar](2000) NULL,
    [Status] [nvarchar](30) NOT NULL CONSTRAINT [DF_Modulos_Status] DEFAULT (N'APROVADO'),
    [CriadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_Modulos_CriadoEm] DEFAULT (sysutcdatetime()),
    [AtualizadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_Modulos_AtualizadoEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_Modulos] PRIMARY KEY CLUSTERED ([ModuloID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[Aulas] (
    [AulaID] [int] IDENTITY(1,1) NOT NULL,
    [ModuloID] [int] NOT NULL,
    [Titulo] [nvarchar](150) NOT NULL,
    [UrlConteudo] [nvarchar](500) NULL,
    [IndiceOrdem] [int] NOT NULL,
    [Status] [nvarchar](30) NOT NULL CONSTRAINT [DF_Aulas_Status] DEFAULT ('PENDENTE'),
    [CriadoPorUsuarioID] [int] NULL,
    [AprovadoPorUsuarioID] [int] NULL,
    [Conteudo] [nvarchar](max) NULL,
    [AtualizadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_Aulas_AtualizadoEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_Aulas] PRIMARY KEY CLUSTERED ([AulaID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO

CREATE TABLE [dbo].[RevisoesConteudo] (
    [RevisaoID] [int] IDENTITY(1,1) NOT NULL,
    [TipoConteudo] [nvarchar](20) NOT NULL,
    [ConteudoID] [int] NOT NULL,
    [Acao] [nvarchar](30) NOT NULL,
    [StatusAnterior] [nvarchar](30) NOT NULL,
    [StatusNovo] [nvarchar](30) NOT NULL,
    [Justificativa] [nvarchar](1000) NULL,
    [UsuarioID] [int] NOT NULL,
    [CriadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_RevisoesConteudo_CriadoEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_RevisoesConteudo] PRIMARY KEY CLUSTERED ([RevisaoID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[UsuarioTrilhas] (
    [UsuarioTrilhaID] [int] IDENTITY(1,1) NOT NULL,
    [UsuarioID] [int] NOT NULL,
    [TrilhaID] [int] NOT NULL,
    [InscritoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_UsuarioTrilhas_InscritoEm] DEFAULT (sysutcdatetime()),
    [ConcluidoEm] [datetime2](7) NULL,
    [ProgressoPercentual] [decimal](5, 2) NOT NULL CONSTRAINT [DF_UsuarioTrilhas_Progresso] DEFAULT ((0.00)),
    [Status] [nvarchar](30) NOT NULL,
    [CanceladoEm] [datetime2](7) NULL,
    CONSTRAINT [PK_UsuarioTrilhas] PRIMARY KEY CLUSTERED ([UsuarioTrilhaID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UQ_Usuario_Trilha] UNIQUE NONCLUSTERED ([UsuarioID] ASC, [TrilhaID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[ProgressoUsuario] (
    [ProgressoID] [int] IDENTITY(1,1) NOT NULL,
    [UsuarioID] [int] NOT NULL,
    [AulaID] [int] NOT NULL,
    [ConcluidoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_ProgressoUsuario_ConcluidoEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_ProgressoUsuario] PRIMARY KEY CLUSTERED ([ProgressoID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UQ_Usuario_Aula] UNIQUE NONCLUSTERED ([UsuarioID] ASC, [AulaID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

-- ============================================================================
-- 3. DOMÍNIO DE AVALIAÇÃO (QUESTÕES, ALTERNATIVAS E PROVAS)
-- ============================================================================

CREATE TABLE [dbo].[Questoes] (
    [QuestaoID] [int] IDENTITY(1,1) NOT NULL,
    [MateriaID] [int] NOT NULL,
    [TextoQuestao] [nvarchar](max) NOT NULL,
    [TipoQuestao] [nvarchar](50) NOT NULL,
    [Peso] [int] NOT NULL CONSTRAINT [DF_Questoes_Peso] DEFAULT ((10)),
    [NivelDificuldade] [nvarchar](20) NOT NULL,
    [Status] [nvarchar](30) NOT NULL CONSTRAINT [DF_Questoes_Status] DEFAULT ('PENDENTE'),
    [ModuloID] [int] NULL,
    [CriadoPorUsuarioID] [int] NULL,
    [AprovadoPorUsuarioID] [int] NULL,
    [TrilhaID] [int] NULL,
    [AulaID] [int] NULL,
    [Explicacao] [nvarchar](2000) NULL,
    [Assunto] [nvarchar](150) NULL,
    [Tags] [nvarchar](500) NULL,
    [CriadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_Questoes_CriadoEm] DEFAULT (sysutcdatetime()),
    [AtualizadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_Questoes_AtualizadoEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_Questoes] PRIMARY KEY CLUSTERED ([QuestaoID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO

CREATE TABLE [dbo].[AlternativasQuestao] (
    [AlternativaID] [int] IDENTITY(1,1) NOT NULL,
    [QuestaoID] [int] NOT NULL,
    [TextoAlternativa] [nvarchar](max) NOT NULL,
    [EhCorreta] [bit] NOT NULL,
    [Explicacao] [nvarchar](1000) NULL,
    CONSTRAINT [PK_AlternativasQuestao] PRIMARY KEY CLUSTERED ([AlternativaID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UQ_AlternativasQuestao_Alternativa_Questao] UNIQUE NONCLUSTERED ([AlternativaID] ASC, [QuestaoID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO

CREATE TABLE [dbo].[Provas] (
    [ProvaID] [int] IDENTITY(1,1) NOT NULL,
    [Titulo] [nvarchar](150) NOT NULL,
    [TipoProva] [nvarchar](30) NOT NULL,
    [MateriaID] [int] NULL,
    [TotalQuestoes] [int] NOT NULL,
    [CriadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_Provas_CriadoEm] DEFAULT (sysutcdatetime()),
    [Descricao] [nvarchar](2000) NULL,
    [TrilhaID] [int] NULL,
    [ModuloID] [int] NULL,
    [TempoLimiteMinutos] [int] NULL,
    [MaxTentativas] [int] NULL,
    [NotaMinimaAprovacao] [int] NOT NULL CONSTRAINT [DF_Provas_NotaMinima] DEFAULT ((60)),
    [RecompensaXP] [int] NOT NULL CONSTRAINT [DF_Provas_RecompensaXP] DEFAULT ((50)),
    [Status] [nvarchar](30) NOT NULL CONSTRAINT [DF_Provas_Status] DEFAULT (N'APROVADO'),
    [CriadoPorUsuarioID] [int] NULL,
    [AtualizadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_Provas_AtualizadoEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_Provas] PRIMARY KEY CLUSTERED ([ProvaID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[ProvaQuestoes] (
    [ProvaID] [int] NOT NULL,
    [QuestaoID] [int] NOT NULL,
    [OrdemQuestao] [int] NOT NULL,
    CONSTRAINT [PK_ProvaQuestoes] PRIMARY KEY CLUSTERED ([ProvaID] ASC, [QuestaoID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UQ_ProvaQuestoes_Ordem] UNIQUE NONCLUSTERED ([ProvaID] ASC, [OrdemQuestao] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[ProvasUsuario] (
    [ProvaUsuarioID] [int] IDENTITY(1,1) NOT NULL,
    [UsuarioID] [int] NOT NULL,
    [ProvaID] [int] NOT NULL,
    [IniciadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_ProvasUsuario_IniciadoEm] DEFAULT (sysutcdatetime()),
    [ConcluidoEm] [datetime2](7) NULL,
    [Pontuacao] [int] NOT NULL CONSTRAINT [DF_ProvasUsuario_Pontuacao] DEFAULT ((0)),
    [Status] [nvarchar](30) NOT NULL,
    [NumeroTentativa] [int] NOT NULL CONSTRAINT [DF_ProvasUsuario_NumeroTentativa] DEFAULT ((1)),
    [PrazoEm] [datetime2](7) NULL,
    [TotalQuestoes] [int] NOT NULL CONSTRAINT [DF_ProvasUsuario_TotalQuestoes] DEFAULT ((0)),
    [NotaPercentual] [decimal](5, 2) NOT NULL CONSTRAINT [DF_ProvasUsuario_Nota] DEFAULT ((0)),
    [XPGanho] [int] NOT NULL CONSTRAINT [DF_ProvasUsuario_XPGanho] DEFAULT ((0)),
    CONSTRAINT [PK_ProvasUsuario] PRIMARY KEY CLUSTERED ([ProvaUsuarioID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[RespostasProvasUsuario] (
    [RespostaUsuarioID] [int] IDENTITY(1,1) NOT NULL,
    [ProvaUsuarioID] [int] NOT NULL,
    [QuestaoID] [int] NOT NULL,
    [AlternativaSelecionadaID] [int] NOT NULL,
    [EhCorreta] [bit] NOT NULL,
    [RespondidoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_RespostasProvas_RespondidoEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_RespostasProvasUsuario] PRIMARY KEY CLUSTERED ([RespostaUsuarioID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UQ_ProvaUsuario_Questao] UNIQUE NONCLUSTERED ([ProvaUsuarioID] ASC, [QuestaoID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

-- ============================================================================
-- 4. DOMÍNIO DE GAMIFICAÇÃO (XP, NÍVEIS, CONQUISTAS E DESAFIOS)
-- ============================================================================

CREATE TABLE [dbo].[NiveisGamificacao] (
    [NivelID] [int] IDENTITY(1,1) NOT NULL,
    [NumeroNivel] [int] NOT NULL,
    [XPMinimo] [int] NOT NULL,
    [TituloNivel] [nvarchar](50) NOT NULL,
    CONSTRAINT [PK_NiveisGamificacao] PRIMARY KEY CLUSTERED ([NivelID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UQ_NiveisGamificacao_XPMinimo] UNIQUE NONCLUSTERED ([XPMinimo] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UQ_NiveisGamificacao_NumeroNivel] UNIQUE NONCLUSTERED ([NumeroNivel] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[XPUsuario] (
    [UsuarioID] [int] NOT NULL,
    [TotalXP] [int] NOT NULL CONSTRAINT [DF_XPUsuario_Total] DEFAULT ((0)),
    [NivelAtualID] [int] NOT NULL,
    [AtualizadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_XPUsuario_AtualizadoEm] DEFAULT (sysutcdatetime()),
    [RowVersion] [timestamp] NOT NULL,
    CONSTRAINT [PK_XPUsuario] PRIMARY KEY CLUSTERED ([UsuarioID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[HistoricoXP] (
    [HistoricoXPID] [int] IDENTITY(1,1) NOT NULL,
    [UsuarioID] [int] NOT NULL,
    [QuantidadeXP] [int] NOT NULL,
    [Motivo] [nvarchar](100) NOT NULL,
    [ReferenciaID] [int] NULL,
    [CriadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_HistoricoXP_CriadoEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_HistoricoXP] PRIMARY KEY CLUSTERED ([HistoricoXPID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[Conquistas] (
    [ConquistaID] [int] IDENTITY(1,1) NOT NULL,
    [NomeConquista] [nvarchar](100) NOT NULL,
    [Descricao] [nvarchar](255) NULL,
    [UrlIcone] [nvarchar](500) NULL,
    CONSTRAINT [PK_Conquistas] PRIMARY KEY CLUSTERED ([ConquistaID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
    CONSTRAINT [UQ_Conquistas_NomeConquista] UNIQUE NONCLUSTERED ([NomeConquista] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[ConquistasUsuario] (
    [UsuarioID] [int] NOT NULL,
    [ConquistaID] [int] NOT NULL,
    [ConquistadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_ConquistasUsuario_ConquistadoEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_ConquistasUsuario] PRIMARY KEY CLUSTERED ([UsuarioID] ASC, [ConquistaID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[UsuarioConquistas] (
    [UsuarioID] [int] NOT NULL,
    [ConquistaID] [int] NOT NULL,
    [ConquistadaEm] [datetime2](7) NOT NULL CONSTRAINT [DF_UsuarioConquistas_ConquistadaEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_UsuarioConquistas] PRIMARY KEY CLUSTERED ([UsuarioID] ASC, [ConquistaID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[Desafios] (
    [DesafioID] [int] IDENTITY(1,1) NOT NULL,
    [Nome] [nvarchar](120) NOT NULL,
    [Descricao] [nvarchar](1000) NOT NULL CONSTRAINT [DF_Desafios_Descricao] DEFAULT (N''),
    [RecompensaXP] [int] NOT NULL,
    [InicioEm] [datetime2](7) NULL,
    [FimEm] [datetime2](7) NULL,
    [Ativo] [bit] NOT NULL CONSTRAINT [DF_Desafios_Ativo] DEFAULT ((1)),
    [CriadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_Desafios_CriadoEm] DEFAULT (sysutcdatetime()),
    [TipoRequisito] [nvarchar](30) NOT NULL CONSTRAINT [DF_Desafios_TipoRequisito] DEFAULT (N'NENHUM'),
    [MetaRequisito] [int] NOT NULL CONSTRAINT [DF_Desafios_MetaRequisito] DEFAULT ((0)),
    [TrilhaID] [int] NULL,
    [ConquistaID] [int] NULL,
    CONSTRAINT [PK_Desafios] PRIMARY KEY CLUSTERED ([DesafioID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[ParticipacoesDesafio] (
    [ParticipacaoID] [int] IDENTITY(1,1) NOT NULL,
    [DesafioID] [int] NOT NULL,
    [UsuarioID] [int] NOT NULL,
    [InscritoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_ParticipacoesDesafio_InscritoEm] DEFAULT (sysutcdatetime()),
    [ConcluidoEm] [datetime2](7) NULL,
    [Status] [nvarchar](30) NOT NULL CONSTRAINT [DF_ParticipacoesDesafio_Status] DEFAULT (N'EM_ANDAMENTO'),
    CONSTRAINT [PK_ParticipacoesDesafio] PRIMARY KEY CLUSTERED ([ParticipacaoID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[RecompensasConcedidas] (
    [RecompensaID] [int] IDENTITY(1,1) NOT NULL,
    [UsuarioID] [int] NOT NULL,
    [TipoOrigem] [nvarchar](20) NOT NULL,
    [OrigemID] [int] NOT NULL,
    [QuantidadeXP] [int] NOT NULL,
    [ConquistaID] [int] NULL,
    [CriadoEm] [datetime2](7) NOT NULL CONSTRAINT [DF_RecompensasConcedidas_CriadoEm] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_RecompensasConcedidas] PRIMARY KEY CLUSTERED ([RecompensaID] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

-- ============================================================================
-- 5. DOMÍNIO DE ASSISTENTE PEDAGÓGICO (CHATBOT IA)
-- ============================================================================

CREATE TABLE [dbo].[ChatConversations] (
    [ConversationId] [uniqueidentifier] NOT NULL,
    [UsuarioID] [int] NOT NULL,
    [CriadoEm] [datetime2](7) NOT NULL,
    [AtualizadoEm] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_ChatConversations] PRIMARY KEY CLUSTERED ([ConversationId] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
GO

CREATE TABLE [dbo].[ChatMessages] (
    [MessageId] [uniqueidentifier] NOT NULL,
    [ConversationId] [uniqueidentifier] NOT NULL,
    [UsuarioID] [int] NOT NULL,
    [Role] [nvarchar](20) NOT NULL,
    [Content] [nvarchar](max) NOT NULL,
    [CreatedAt] [datetime2](7) NOT NULL CONSTRAINT [DF_ChatMessages_CreatedAt] DEFAULT (sysutcdatetime()),
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY CLUSTERED ([MessageId] ASC)
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
GO