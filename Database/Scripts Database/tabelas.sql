--------------------------------------------------------------------------------
-- PROJETO DUCKS - SCRIPT DE CRIAÇÃO DAS TABELAS
--------------------------------------------------------------------------------

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Tabela de Utilizadores da Plataforma
CREATE TABLE [dbo].[Usuarios](
    [UsuarioID] [int] IDENTITY(1,1) NOT NULL,
    [NomeUsuario] [nvarchar](100) NOT NULL,
    [Email] [nvarchar](255) NOT NULL,
    [HashSenha] [varbinary](64) NOT NULL,
    [SaltSenha] [varbinary](32) NOT NULL,
    [EstaAtivo] [bit] NOT NULL,
    [CriadoEm] [datetime2](7) NOT NULL,
    [AtualizadoEm] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED ([UsuarioID] ASC),
UNIQUE NONCLUSTERED ([NomeUsuario] ASC),
UNIQUE NONCLUSTERED ([Email] ASC)
);
GO

-- 2. Tabela de Papéis (RBAC)
CREATE TABLE [dbo].[Papeis](
    [PapelID] [int] IDENTITY(1,1) NOT NULL,
    [NomePapel] [nvarchar](50) NOT NULL,
    [Descricao] [nvarchar](255) NULL,
    [CriadoEm] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED ([PapelID] ASC),
UNIQUE NONCLUSTERED ([NomePapel] ASC)
);
GO

-- 3. Associação N:N entre Utilizadores e Papéis
CREATE TABLE [dbo].[UsuarioPapeis](
    [UsuarioID] [int] NOT NULL,
    [PapelID] [int] NOT NULL,
    [AtribuidoEm] [datetime2](7) NOT NULL,
CONSTRAINT [PK_UsuarioPapeis] PRIMARY KEY CLUSTERED ([UsuarioID] ASC, [PapelID] ASC)
);
GO

-- 4. Matérias / Disciplinas
CREATE TABLE [dbo].[Materias](
    [MateriaID] [int] IDENTITY(1,1) NOT NULL,
    [NomeMateria] [nvarchar](100) NOT NULL,
    [Descricao] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED ([MateriaID] ASC),
UNIQUE NONCLUSTERED ([NomeMateria] ASC)
);
GO

-- 5. Trilhas de Aprendizagem
CREATE TABLE [dbo].[TrilhasAprendizagem](
    [TrilhaID] [int] IDENTITY(1,1) NOT NULL,
    [NomeTrilha] [nvarchar](150) NOT NULL,
    [Descricao] [nvarchar](max) NULL,
    [MateriaID] [int] NOT NULL,
    [Status] [nvarchar](30) NOT NULL,
    [CriadoPorUsuarioID] [int] NULL,
    [AprovadoPorUsuarioID] [int] NULL,
    [AtualizadoEm] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED ([TrilhaID] ASC)
);
GO

-- 6. Módulos das Trilhas
CREATE TABLE [dbo].[Modulos](
    [ModuloID] [int] IDENTITY(1,1) NOT NULL,
    [TrilhaID] [int] NOT NULL,
    [Titulo] [nvarchar](150) NOT NULL,
    [IndiceOrdem] [int] NOT NULL,
PRIMARY KEY CLUSTERED ([ModuloID] ASC)
);
GO

-- 7. Aulas dos Módulos
CREATE TABLE [dbo].[Aulas](
    [AulaID] [int] IDENTITY(1,1) NOT NULL,
    [ModuloID] [int] NOT NULL,
    [Titulo] [nvarchar](150) NOT NULL,
    [UrlConteudo] [nvarchar](500) NULL,
    [IndiceOrdem] [int] NOT NULL,
    [Status] [nvarchar](30) NOT NULL,
    [CriadoPorUsuarioID] [int] NULL,
    [AprovadoPorUsuarioID] [int] NULL,
PRIMARY KEY CLUSTERED ([AulaID] ASC)
);
GO

-- 8. Progresso Individual nas Aulas
CREATE TABLE [dbo].[ProgressoUsuario](
    [ProgressoID] [int] IDENTITY(1,1) NOT NULL,
    [UsuarioID] [int] NOT NULL,
    [AulaID] [int] NOT NULL,
    [ConcluidoEm] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED ([ProgressoID] ASC),
CONSTRAINT [UQ_Usuario_Aula] UNIQUE NONCLUSTERED ([UsuarioID] ASC, [AulaID] ASC)
);
GO

-- 9. Inscrições em Trilhas
CREATE TABLE [dbo].[UsuarioTrilhas](
    [UsuarioTrilhaID] [int] IDENTITY(1,1) NOT NULL,
    [UsuarioID] [int] NOT NULL,
    [TrilhaID] [int] NOT NULL,
    [InscritoEm] [datetime2](7) NOT NULL,
    [ConcluidoEm] [datetime2](7) NULL,
    [ProgressoPercentual] [decimal](5, 2) NOT NULL,
    [Status] [nvarchar](30) NOT NULL,
PRIMARY KEY CLUSTERED ([UsuarioTrilhaID] ASC),
CONSTRAINT [UQ_Usuario_Trilha] UNIQUE NONCLUSTERED ([UsuarioID] ASC, [TrilhaID] ASC)
);
GO

-- 10. Banco de Questões
CREATE TABLE [dbo].[Questoes](
    [QuestaoID] [int] IDENTITY(1,1) NOT NULL,
    [MateriaID] [int] NOT NULL,
    [TextoQuestao] [nvarchar](max) NOT NULL,
    [TipoQuestao] [nvarchar](50) NOT NULL,
    [Peso] [int] NOT NULL,
    [NivelDificuldade] [nvarchar](20) NOT NULL,
    [Status] [nvarchar](30) NOT NULL,
    [ModuloID] [int] NULL,
    [CriadoPorUsuarioID] [int] NULL,
    [AprovadoPorUsuarioID] [int] NULL,
PRIMARY KEY CLUSTERED ([QuestaoID] ASC)
);
GO

-- 11. Alternativas das Questões
CREATE TABLE [dbo].[AlternativasQuestao](
    [AlternativaID] [int] IDENTITY(1,1) NOT NULL,
    [QuestaoID] [int] NOT NULL,
    [TextoAlternativa] [nvarchar](max) NOT NULL,
    [EhCorreta] [bit] NOT NULL,
PRIMARY KEY CLUSTERED ([AlternativaID] ASC),
CONSTRAINT [UQ_AlternativasQuestao_Alternativa_Questao] UNIQUE NONCLUSTERED ([AlternativaID] ASC, [QuestaoID] ASC)
);
GO

-- 12. Provas / Simulados
CREATE TABLE [dbo].[Provas](
    [ProvaID] [int] IDENTITY(1,1) NOT NULL,
    [Titulo] [nvarchar](150) NOT NULL,
    [TipoProva] [nvarchar](30) NOT NULL,
    [MateriaID] [int] NULL,
    [TotalQuestoes] [int] NOT NULL,
    [CriadoEm] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED ([ProvaID] ASC)
);
GO

-- 13. Composição de Questões por Prova
CREATE TABLE [dbo].[ProvaQuestoes](
    [ProvaID] [int] NOT NULL,
    [QuestaoID] [int] NOT NULL,
    [OrdemQuestao] [int] NOT NULL,
CONSTRAINT [PK_ProvaQuestoes] PRIMARY KEY CLUSTERED ([ProvaID] ASC, [QuestaoID] ASC),
CONSTRAINT [UQ_ProvaQuestoes_Ordem] UNIQUE NONCLUSTERED ([ProvaID] ASC, [OrdemQuestao] ASC)
);
GO

-- 14. Tentativas de Provas por Utilizadores
CREATE TABLE [dbo].[ProvasUsuario](
    [ProvaUsuarioID] [int] IDENTITY(1,1) NOT NULL,
    [UsuarioID] [int] NOT NULL,
    [ProvaID] [int] NOT NULL,
    [IniciadoEm] [datetime2](7) NOT NULL,
    [ConcluidoEm] [datetime2](7) NULL,
    [Pontuacao] [int] NOT NULL,
    [Status] [nvarchar](30) NOT NULL,
PRIMARY KEY CLUSTERED ([ProvaUsuarioID] ASC)
);
GO

-- 15. Respostas Submetidas pelos Utilizadores nas Provas
CREATE TABLE [dbo].[RespostasProvasUsuario](
    [RespostaUsuarioID] [int] IDENTITY(1,1) NOT NULL,
    [ProvaUsuarioID] [int] NOT NULL,
    [QuestaoID] [int] NOT NULL,
    [AlternativaSelecionadaID] [int] NOT NULL,
    [EhCorreta] [bit] NOT NULL,
    [RespondidoEm] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED ([RespostaUsuarioID] ASC),
CONSTRAINT [UQ_ProvaUsuario_Questao] UNIQUE NONCLUSTERED ([ProvaUsuarioID] ASC, [QuestaoID] ASC)
);
GO

-- 16. Níveis de Gamificação
CREATE TABLE [dbo].[NiveisGamificacao](
    [NivelID] [int] IDENTITY(1,1) NOT NULL,
    [NumeroNivel] [int] NOT NULL,
    [XPMinimo] [int] NOT NULL,
    [TituloNivel] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED ([NivelID] ASC),
UNIQUE NONCLUSTERED ([XPMinimo] ASC),
UNIQUE NONCLUSTERED ([NumeroNivel] ASC)
);
GO

-- 17. Saldo e Nível de XP Atual do Utilizador
CREATE TABLE [dbo].[XPUsuario](
    [UsuarioID] [int] NOT NULL,
    [TotalXP] [int] NOT NULL,
    [NivelAtualID] [int] NOT NULL,
    [AtualizadoEm] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED ([UsuarioID] ASC)
);
GO

-- 18. Histórico de Ganhos de XP
CREATE TABLE [dbo].[HistoricoXP](
    [HistoricoXPID] [int] IDENTITY(1,1) NOT NULL,
    [UsuarioID] [int] NOT NULL,
    [QuantidadeXP] [int] NOT NULL,
    [Motivo] [nvarchar](100) NOT NULL,
    [ReferenciaID] [int] NULL,
    [CriadoEm] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED ([HistoricoXPID] ASC)
);
GO

-- 19. Catálogo de Conquistas / Medalhas
CREATE TABLE [dbo].[Conquistas](
    [ConquistaID] [int] IDENTITY(1,1)NOT NULL,
    [NomeConquista] [nvarchar](100) NOT NULL,
    [Descricao] [nvarchar](255) NULL,
    [UrlIcone] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED ([ConquistaID] ASC),
UNIQUE NONCLUSTERED ([NomeConquista] ASC)
);
GO

-- 20. Conquistas Desbloqueadas pelos Utilizadores
CREATE TABLE [dbo].[ConquistasUsuario](
    [UsuarioID] [int] NOT NULL,
    [ConquistaID] [int] NOT NULL,
    [ConquistadoEm] [datetime2](7) NOT NULL,
CONSTRAINT [PK_ConquistasUsuario] PRIMARY KEY CLUSTERED ([UsuarioID] ASC, [ConquistaID] ASC)
);
GO

-- 21. Registo de Auditoria
CREATE TABLE [dbo].[LogsAuditoria](
    [LogID] [int] IDENTITY(1,1) NOT NULL,
    [UsuarioID] [int] NULL,
    [Acao] [nvarchar](100) NOT NULL,
    [Detalhes] [nvarchar](max) NULL,
    [EnderecoIP] [nvarchar](50) NULL,
    [CriadoEm] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED ([LogID] ASC)
);
GO

-- 22. Configurações Globais da Plataforma
CREATE TABLE [dbo].[ConfiguracoesPlataforma](
    [ConfiguracaoID] [int] IDENTITY(1,1) NOT NULL,
    [Chave] [nvarchar](100) NOT NULL,
    [Valor] [nvarchar](max) NOT NULL,
    [Descricao] [nvarchar](255) NULL,
    [AtualizadoEm] [datetime2](7) NOT NULL,
    [AtualizadoPorUsuarioID] [int] NULL,
PRIMARY KEY CLUSTERED ([ConfiguracaoID] ASC),
UNIQUE NONCLUSTERED ([Chave] ASC)
);
GO