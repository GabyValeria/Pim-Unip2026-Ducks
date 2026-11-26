-- ============================================================================
-- PROJETO DUCKS - BANCO DE DADOS [ducks-db]
-- Arquivo: restricoes_e_indices.sql
-- Descrição: Criação de todas as Foreign Keys, Check Constraints e Índices
--            (simples, compostos, únicos e filtrados).
-- Ordem: Executado após tabelas.sql e antes de procedimentos.sql.
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO

-- ============================================================================
-- 1. FOREIGN KEYS (CHAVES ESTRANGEIRAS)
-- ============================================================================

-- AlternativasQuestao
ALTER TABLE [dbo].[AlternativasQuestao] WITH CHECK ADD CONSTRAINT [FK_Alternativas_Questao]
    FOREIGN KEY([QuestaoID]) REFERENCES [dbo].[Questoes] ([QuestaoID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[AlternativasQuestao] CHECK CONSTRAINT [FK_Alternativas_Questao];
GO

-- Aulas
ALTER TABLE [dbo].[Aulas] WITH CHECK ADD CONSTRAINT [FK_Aulas_Aprovador]
    FOREIGN KEY([AprovadoPorUsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
GO
ALTER TABLE [dbo].[Aulas] CHECK CONSTRAINT [FK_Aulas_Aprovador];
GO

ALTER TABLE [dbo].[Aulas] WITH CHECK ADD CONSTRAINT [FK_Aulas_Criador]
    FOREIGN KEY([CriadoPorUsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
GO
ALTER TABLE [dbo].[Aulas] CHECK CONSTRAINT [FK_Aulas_Criador];
GO

ALTER TABLE [dbo].[Aulas] WITH CHECK ADD CONSTRAINT [FK_Aulas_Modulo]
    FOREIGN KEY([ModuloID]) REFERENCES [dbo].[Modulos] ([ModuloID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[Aulas] CHECK CONSTRAINT [FK_Aulas_Modulo];
GO

-- ChatConversations & ChatMessages
ALTER TABLE [dbo].[ChatConversations] WITH CHECK ADD CONSTRAINT [FK_ChatConversations_Usuarios]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
GO
ALTER TABLE [dbo].[ChatConversations] CHECK CONSTRAINT [FK_ChatConversations_Usuarios];
GO

-- Nota: Requer o índice único UX_ChatConversations_Conversation_User criado na seção de índices abaixo
-- Caso execute em bloco único, os índices únicos necessários são criados antes desta FK:
CREATE UNIQUE NONCLUSTERED INDEX [UX_ChatConversations_Conversation_User] ON [dbo].[ChatConversations]
(
    [ConversationId] ASC,
    [UsuarioID] ASC
) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

ALTER TABLE [dbo].[ChatMessages] WITH CHECK ADD CONSTRAINT [FK_ChatMessages_Conversation_User]
    FOREIGN KEY([ConversationId], [UsuarioID]) REFERENCES [dbo].[ChatConversations] ([ConversationId], [UsuarioID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[ChatMessages] CHECK CONSTRAINT [FK_ChatMessages_Conversation_User];
GO

-- ConfiguracoesPlataforma
ALTER TABLE [dbo].[ConfiguracoesPlataforma] WITH CHECK ADD CONSTRAINT [FK_Config_Usuario]
    FOREIGN KEY([AtualizadoPorUsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE SET NULL;
GO
ALTER TABLE [dbo].[ConfiguracoesPlataforma] CHECK CONSTRAINT [FK_Config_Usuario];
GO

-- ConquistasUsuario & UsuarioConquistas
ALTER TABLE [dbo].[ConquistasUsuario] WITH CHECK ADD CONSTRAINT [FK_ConquistasUsuario_Conquista]
    FOREIGN KEY([ConquistaID]) REFERENCES [dbo].[Conquistas] ([ConquistaID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[ConquistasUsuario] CHECK CONSTRAINT [FK_ConquistasUsuario_Conquista];
GO

ALTER TABLE [dbo].[ConquistasUsuario] WITH CHECK ADD CONSTRAINT [FK_ConquistasUsuario_Usuario]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[ConquistasUsuario] CHECK CONSTRAINT [FK_ConquistasUsuario_Usuario];
GO

ALTER TABLE [dbo].[UsuarioConquistas] WITH CHECK ADD CONSTRAINT [FK_UsuarioConquistas_Conquistas]
    FOREIGN KEY([ConquistaID]) REFERENCES [dbo].[Conquistas] ([ConquistaID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[UsuarioConquistas] CHECK CONSTRAINT [FK_UsuarioConquistas_Conquistas];
GO

ALTER TABLE [dbo].[UsuarioConquistas] WITH CHECK ADD CONSTRAINT [FK_UsuarioConquistas_Usuarios]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[UsuarioConquistas] CHECK CONSTRAINT [FK_UsuarioConquistas_Usuarios];
GO

-- Desafios & ParticipacoesDesafio
ALTER TABLE [dbo].[Desafios] WITH NOCHECK ADD CONSTRAINT [FK_Desafios_Conquistas]
    FOREIGN KEY([ConquistaID]) REFERENCES [dbo].[Conquistas] ([ConquistaID]) ON DELETE SET NULL;
GO
ALTER TABLE [dbo].[Desafios] CHECK CONSTRAINT [FK_Desafios_Conquistas];
GO

ALTER TABLE [dbo].[Desafios] WITH NOCHECK ADD CONSTRAINT [FK_Desafios_TrilhasAprendizagem]
    FOREIGN KEY([TrilhaID]) REFERENCES [dbo].[TrilhasAprendizagem] ([TrilhaID]) ON DELETE SET NULL;
GO
ALTER TABLE [dbo].[Desafios] CHECK CONSTRAINT [FK_Desafios_TrilhasAprendizagem];
GO

ALTER TABLE [dbo].[ParticipacoesDesafio] WITH CHECK ADD CONSTRAINT [FK_ParticipacoesDesafio_Desafios]
    FOREIGN KEY([DesafioID]) REFERENCES [dbo].[Desafios] ([DesafioID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[ParticipacoesDesafio] CHECK CONSTRAINT [FK_ParticipacoesDesafio_Desafios];
GO

ALTER TABLE [dbo].[ParticipacoesDesafio] WITH CHECK ADD CONSTRAINT [FK_ParticipacoesDesafio_Usuarios]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[ParticipacoesDesafio] CHECK CONSTRAINT [FK_ParticipacoesDesafio_Usuarios];
GO

-- HistoricoXP & XPUsuario
ALTER TABLE [dbo].[HistoricoXP] WITH CHECK ADD CONSTRAINT [FK_HistoricoXP_Usuario]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[HistoricoXP] CHECK CONSTRAINT [FK_HistoricoXP_Usuario];
GO

ALTER TABLE [dbo].[XPUsuario] WITH CHECK ADD CONSTRAINT [FK_XPUsuario_Nivel]
    FOREIGN KEY([NivelAtualID]) REFERENCES [dbo].[NiveisGamificacao] ([NivelID]);
GO
ALTER TABLE [dbo].[XPUsuario] CHECK CONSTRAINT [FK_XPUsuario_Nivel];
GO

ALTER TABLE [dbo].[XPUsuario] WITH CHECK ADD CONSTRAINT [FK_XPUsuario_Usuario]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[XPUsuario] CHECK CONSTRAINT [FK_XPUsuario_Usuario];
GO

-- RecompensasConcedidas
ALTER TABLE [dbo].[RecompensasConcedidas] WITH CHECK ADD CONSTRAINT [FK_RecompensasConcedidas_Usuarios]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[RecompensasConcedidas] CHECK CONSTRAINT [FK_RecompensasConcedidas_Usuarios];
GO

-- LogsAuditoria & RevisoesConteudo
ALTER TABLE [dbo].[LogsAuditoria] WITH CHECK ADD CONSTRAINT [FK_LogsAuditoria_Usuario]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE SET NULL;
GO
ALTER TABLE [dbo].[LogsAuditoria] CHECK CONSTRAINT [FK_LogsAuditoria_Usuario];
GO

ALTER TABLE [dbo].[RevisoesConteudo] WITH CHECK ADD CONSTRAINT [FK_RevisoesConteudo_Usuarios]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
GO
ALTER TABLE [dbo].[RevisoesConteudo] CHECK CONSTRAINT [FK_RevisoesConteudo_Usuarios];
GO

-- TrilhasAprendizagem, Modulos, UsuarioTrilhas & ProgressoUsuario
ALTER TABLE [dbo].[TrilhasAprendizagem] WITH CHECK ADD CONSTRAINT [FK_Trilhas_Aprovador]
    FOREIGN KEY([AprovadoPorUsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
GO
ALTER TABLE [dbo].[TrilhasAprendizagem] CHECK CONSTRAINT [FK_Trilhas_Aprovador];
GO

ALTER TABLE [dbo].[TrilhasAprendizagem] WITH CHECK ADD CONSTRAINT [FK_Trilhas_Criador]
    FOREIGN KEY([CriadoPorUsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
GO
ALTER TABLE [dbo].[TrilhasAprendizagem] CHECK CONSTRAINT [FK_Trilhas_Criador];
GO

ALTER TABLE [dbo].[TrilhasAprendizagem] WITH CHECK ADD CONSTRAINT [FK_Trilhas_Materia]
    FOREIGN KEY([MateriaID]) REFERENCES [dbo].[Materias] ([MateriaID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[TrilhasAprendizagem] CHECK CONSTRAINT [FK_Trilhas_Materia];
GO

ALTER TABLE [dbo].[Modulos] WITH CHECK ADD CONSTRAINT [FK_Modulos_Trilha]
    FOREIGN KEY([TrilhaID]) REFERENCES [dbo].[TrilhasAprendizagem] ([TrilhaID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[Modulos] CHECK CONSTRAINT [FK_Modulos_Trilha];
GO

ALTER TABLE [dbo].[UsuarioTrilhas] WITH CHECK ADD CONSTRAINT [FK_UsuarioTrilhas_Trilha]
    FOREIGN KEY([TrilhaID]) REFERENCES [dbo].[TrilhasAprendizagem] ([TrilhaID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[UsuarioTrilhas] CHECK CONSTRAINT [FK_UsuarioTrilhas_Trilha];
GO

ALTER TABLE [dbo].[UsuarioTrilhas] WITH CHECK ADD CONSTRAINT [FK_UsuarioTrilhas_Usuario]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[UsuarioTrilhas] CHECK CONSTRAINT [FK_UsuarioTrilhas_Usuario];
GO

ALTER TABLE [dbo].[ProgressoUsuario] WITH CHECK ADD CONSTRAINT [FK_Progresso_Aula]
    FOREIGN KEY([AulaID]) REFERENCES [dbo].[Aulas] ([AulaID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[ProgressoUsuario] CHECK CONSTRAINT [FK_Progresso_Aula];
GO

ALTER TABLE [dbo].[ProgressoUsuario] WITH CHECK ADD CONSTRAINT [FK_Progresso_Usuario]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[ProgressoUsuario] CHECK CONSTRAINT [FK_Progresso_Usuario];
GO

-- Questoes, Provas, ProvaQuestoes, ProvasUsuario & RespostasProvasUsuario
ALTER TABLE [dbo].[Questoes] WITH CHECK ADD CONSTRAINT [FK_Questoes_Aprovador]
    FOREIGN KEY([AprovadoPorUsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
GO
ALTER TABLE [dbo].[Questoes] CHECK CONSTRAINT [FK_Questoes_Aprovador];
GO

ALTER TABLE [dbo].[Questoes] WITH NOCHECK ADD CONSTRAINT [FK_Questoes_Aulas]
    FOREIGN KEY([AulaID]) REFERENCES [dbo].[Aulas] ([AulaID]);
GO
ALTER TABLE [dbo].[Questoes] CHECK CONSTRAINT [FK_Questoes_Aulas];
GO

ALTER TABLE [dbo].[Questoes] WITH CHECK ADD CONSTRAINT [FK_Questoes_Criador]
    FOREIGN KEY([CriadoPorUsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
GO
ALTER TABLE [dbo].[Questoes] CHECK CONSTRAINT [FK_Questoes_Criador];
GO

ALTER TABLE [dbo].[Questoes] WITH CHECK ADD CONSTRAINT [FK_Questoes_Materia]
    FOREIGN KEY([MateriaID]) REFERENCES [dbo].[Materias] ([MateriaID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[Questoes] CHECK CONSTRAINT [FK_Questoes_Materia];
GO

ALTER TABLE [dbo].[Questoes] WITH CHECK ADD CONSTRAINT [FK_Questoes_Modulo]
    FOREIGN KEY([ModuloID]) REFERENCES [dbo].[Modulos] ([ModuloID]);
GO
ALTER TABLE [dbo].[Questoes] CHECK CONSTRAINT [FK_Questoes_Modulo];
GO

ALTER TABLE [dbo].[Questoes] WITH NOCHECK ADD CONSTRAINT [FK_Questoes_TrilhasAprendizagem]
    FOREIGN KEY([TrilhaID]) REFERENCES [dbo].[TrilhasAprendizagem] ([TrilhaID]);
GO
ALTER TABLE [dbo].[Questoes] CHECK CONSTRAINT [FK_Questoes_TrilhasAprendizagem];
GO

ALTER TABLE [dbo].[Provas] WITH CHECK ADD CONSTRAINT [FK_Provas_Materia]
    FOREIGN KEY([MateriaID]) REFERENCES [dbo].[Materias] ([MateriaID]);
GO
ALTER TABLE [dbo].[Provas] CHECK CONSTRAINT [FK_Provas_Materia];
GO

ALTER TABLE [dbo].[Provas] WITH NOCHECK ADD CONSTRAINT [FK_Provas_Modulos]
    FOREIGN KEY([ModuloID]) REFERENCES [dbo].[Modulos] ([ModuloID]);
GO
ALTER TABLE [dbo].[Provas] CHECK CONSTRAINT [FK_Provas_Modulos];
GO

ALTER TABLE [dbo].[Provas] WITH NOCHECK ADD CONSTRAINT [FK_Provas_TrilhasAprendizagem]
    FOREIGN KEY([TrilhaID]) REFERENCES [dbo].[TrilhasAprendizagem] ([TrilhaID]);
GO
ALTER TABLE [dbo].[Provas] CHECK CONSTRAINT [FK_Provas_TrilhasAprendizagem];
GO

ALTER TABLE [dbo].[ProvaQuestoes] WITH CHECK ADD CONSTRAINT [FK_ProvaQuestoes_Prova]
    FOREIGN KEY([ProvaID]) REFERENCES [dbo].[Provas] ([ProvaID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[ProvaQuestoes] CHECK CONSTRAINT [FK_ProvaQuestoes_Prova];
GO

ALTER TABLE [dbo].[ProvaQuestoes] WITH CHECK ADD CONSTRAINT [FK_ProvaQuestoes_Questao]
    FOREIGN KEY([QuestaoID]) REFERENCES [dbo].[Questoes] ([QuestaoID]);
GO
ALTER TABLE [dbo].[ProvaQuestoes] CHECK CONSTRAINT [FK_ProvaQuestoes_Questao];
GO

ALTER TABLE [dbo].[ProvasUsuario] WITH CHECK ADD CONSTRAINT [FK_ProvasUsuario_Prova]
    FOREIGN KEY([ProvaID]) REFERENCES [dbo].[Provas] ([ProvaID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[ProvasUsuario] CHECK CONSTRAINT [FK_ProvasUsuario_Prova];
GO

ALTER TABLE [dbo].[ProvasUsuario] WITH CHECK ADD CONSTRAINT [FK_ProvasUsuario_Usuario]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[ProvasUsuario] CHECK CONSTRAINT [FK_ProvasUsuario_Usuario];
GO

ALTER TABLE [dbo].[RespostasProvasUsuario] WITH CHECK ADD CONSTRAINT [FK_Respostas_Alternativa_Questao]
    FOREIGN KEY([AlternativaSelecionadaID], [QuestaoID]) REFERENCES [dbo].[AlternativasQuestao] ([AlternativaID], [QuestaoID]);
GO
ALTER TABLE [dbo].[RespostasProvasUsuario] CHECK CONSTRAINT [FK_Respostas_Alternativa_Questao];
GO

ALTER TABLE [dbo].[RespostasProvasUsuario] WITH CHECK ADD CONSTRAINT [FK_Respostas_ProvaUsuario]
    FOREIGN KEY([ProvaUsuarioID]) REFERENCES [dbo].[ProvasUsuario] ([ProvaUsuarioID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[RespostasProvasUsuario] CHECK CONSTRAINT [FK_Respostas_ProvaUsuario];
GO

ALTER TABLE [dbo].[RespostasProvasUsuario] WITH CHECK ADD CONSTRAINT [FK_Respostas_Questao]
    FOREIGN KEY([QuestaoID]) REFERENCES [dbo].[Questoes] ([QuestaoID]);
GO
ALTER TABLE [dbo].[RespostasProvasUsuario] CHECK CONSTRAINT [FK_Respostas_Questao];
GO

-- Tokens & UsuarioPapeis
ALTER TABLE [dbo].[RefreshTokens] WITH CHECK ADD CONSTRAINT [FK_RefreshTokens_Usuarios]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
GO
ALTER TABLE [dbo].[RefreshTokens] CHECK CONSTRAINT [FK_RefreshTokens_Usuarios];
GO

ALTER TABLE [dbo].[TokensRecuperacaoSenha] WITH CHECK ADD CONSTRAINT [FK_TokensRecuperacaoSenha_Usuarios]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[TokensRecuperacaoSenha] CHECK CONSTRAINT [FK_TokensRecuperacaoSenha_Usuarios];
GO

ALTER TABLE [dbo].[UsuarioPapeis] WITH CHECK ADD CONSTRAINT [FK_UsuarioPapeis_Papel]
    FOREIGN KEY([PapelID]) REFERENCES [dbo].[Papeis] ([PapelID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[UsuarioPapeis] CHECK CONSTRAINT [FK_UsuarioPapeis_Papel];
GO

ALTER TABLE [dbo].[UsuarioPapeis] WITH CHECK ADD CONSTRAINT [FK_UsuarioPapeis_Usuario]
    FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
GO
ALTER TABLE [dbo].[UsuarioPapeis] CHECK CONSTRAINT [FK_UsuarioPapeis_Usuario];
GO

-- ============================================================================
-- 2. CHECK CONSTRAINTS (REGRAS DE VALIDAÇÃO DE DOMÍNIO)
-- ============================================================================

-- Aulas & TrilhasAprendizagem
ALTER TABLE [dbo].[Aulas] WITH NOCHECK ADD CONSTRAINT [CK_Aulas_StatusConteudo]
    CHECK ([Status] IN (N'RASCUNHO', N'PENDENTE', N'APROVADO', N'REJEITADO', N'REMOVIDO'));
GO
ALTER TABLE [dbo].[Aulas] CHECK CONSTRAINT [CK_Aulas_StatusConteudo];
GO

ALTER TABLE [dbo].[TrilhasAprendizagem] WITH NOCHECK ADD CONSTRAINT [CK_TrilhasAprendizagem_StatusConteudo]
    CHECK ([Status] IN (N'RASCUNHO', N'PENDENTE', N'APROVADO', N'REJEITADO', N'REMOVIDO'));
GO
ALTER TABLE [dbo].[TrilhasAprendizagem] CHECK CONSTRAINT [CK_TrilhasAprendizagem_StatusConteudo];
GO

-- ChatMessages
ALTER TABLE [dbo].[ChatMessages] WITH CHECK ADD CONSTRAINT [CK_ChatMessages_Content]
    CHECK (LEN([Content]) > 0);
GO
ALTER TABLE [dbo].[ChatMessages] CHECK CONSTRAINT [CK_ChatMessages_Content];
GO

ALTER TABLE [dbo].[ChatMessages] WITH CHECK ADD CONSTRAINT [CK_ChatMessages_Role]
    CHECK ([Role] IN (N'user', N'assistant', N'system'));
GO
ALTER TABLE [dbo].[ChatMessages] CHECK CONSTRAINT [CK_ChatMessages_Role];
GO

-- Desafios
ALTER TABLE [dbo].[Desafios] WITH CHECK ADD CONSTRAINT [CK_Desafios_Periodo]
    CHECK ([FimEm] IS NULL OR [InicioEm] IS NULL OR [FimEm] > [InicioEm]);
GO
ALTER TABLE [dbo].[Desafios] CHECK CONSTRAINT [CK_Desafios_Periodo];
GO

ALTER TABLE [dbo].[Desafios] WITH CHECK ADD CONSTRAINT [CK_Desafios_RecompensaXP]
    CHECK ([RecompensaXP] >= 0);
GO
ALTER TABLE [dbo].[Desafios] CHECK CONSTRAINT [CK_Desafios_RecompensaXP];
GO

-- NiveisGamificacao & XPUsuario
ALTER TABLE [dbo].[NiveisGamificacao] WITH CHECK ADD CONSTRAINT [CK_NiveisGamificacao_Limites]
    CHECK ([NumeroNivel] > 0 AND [XPMinimo] >= 0);
GO
ALTER TABLE [dbo].[NiveisGamificacao] CHECK CONSTRAINT [CK_NiveisGamificacao_Limites];
GO

ALTER TABLE [dbo].[XPUsuario] WITH CHECK ADD CONSTRAINT [CK_XPUsuario_TotalXP_NaoNegativo]
    CHECK ([TotalXP] >= 0);
GO
ALTER TABLE [dbo].[XPUsuario] CHECK CONSTRAINT [CK_XPUsuario_TotalXP_NaoNegativo];
GO

-- Provas & ProvasUsuario
ALTER TABLE [dbo].[Provas] WITH CHECK ADD CONSTRAINT [CK_Provas_MaxTentativas]
    CHECK ([MaxTentativas] IS NULL OR [MaxTentativas] >= 1);
GO
ALTER TABLE [dbo].[Provas] CHECK CONSTRAINT [CK_Provas_MaxTentativas];
GO

ALTER TABLE [dbo].[Provas] WITH CHECK ADD CONSTRAINT [CK_Provas_TempoLimite]
    CHECK ([TempoLimiteMinutos] IS NULL OR ([TempoLimiteMinutos] >= 1 AND [TempoLimiteMinutos] <= 600));
GO
ALTER TABLE [dbo].[Provas] CHECK CONSTRAINT [CK_Provas_TempoLimite];
GO

ALTER TABLE [dbo].[Provas] WITH CHECK ADD CONSTRAINT [CK_TipoProva]
    CHECK ([TipoProva] IN ('GERAL', 'ESPECIFICA'));
GO
ALTER TABLE [dbo].[Provas] CHECK CONSTRAINT [CK_TipoProva];
GO

ALTER TABLE [dbo].[Provas] WITH CHECK ADD CONSTRAINT [CK_Provas_Tipo_Materia]
    CHECK (([TipoProva] = N'GERAL' AND [MateriaID] IS NULL) OR ([TipoProva] = N'ESPECIFICA' AND [MateriaID] IS NOT NULL));
GO
ALTER TABLE [dbo].[Provas] CHECK CONSTRAINT [CK_Provas_Tipo_Materia];
GO

ALTER TABLE [dbo].[Provas] WITH CHECK ADD CONSTRAINT [CK_Provas_TotalQuestoes_Positivo]
    CHECK ([TotalQuestoes] > 0);
GO
ALTER TABLE [dbo].[Provas] CHECK CONSTRAINT [CK_Provas_TotalQuestoes_Positivo];
GO

ALTER TABLE [dbo].[ProvasUsuario] WITH CHECK ADD CONSTRAINT [CK_StatusProvaUsuario]
    CHECK ([Status] IN ('EM_ANDAMENTO', 'CONCLUIDA', 'ABANDONADA'));
GO
ALTER TABLE [dbo].[ProvasUsuario] CHECK CONSTRAINT [CK_StatusProvaUsuario];
GO

-- Questoes
ALTER TABLE [dbo].[Questoes] WITH CHECK ADD CONSTRAINT [CK_Dificuldade]
    CHECK ([NivelDificuldade] IN ('FACIL', 'MEDIO', 'DIFICIL'));
GO
ALTER TABLE [dbo].[Questoes] CHECK CONSTRAINT [CK_Dificuldade];
GO

ALTER TABLE [dbo].[Questoes] WITH CHECK ADD CONSTRAINT [CK_Questoes_Peso_Positivo]
    CHECK ([Peso] > 0);
GO
ALTER TABLE [dbo].[Questoes] CHECK CONSTRAINT [CK_Questoes_Peso_Positivo];
GO

ALTER TABLE [dbo].[Questoes] WITH CHECK ADD CONSTRAINT [CK_TipoQuestao]
    CHECK ([TipoQuestao] IN ('MULTIPLA_ESCOLHA', 'VERDADEIRO_FALSO'));
GO
ALTER TABLE [dbo].[Questoes] CHECK CONSTRAINT [CK_TipoQuestao];
GO

-- RecompensasConcedidas
ALTER TABLE [dbo].[RecompensasConcedidas] WITH CHECK ADD CONSTRAINT [CK_RecompensasConcedidas_Tipo]
    CHECK ([TipoOrigem] IN (N'AULA', N'TRILHA', N'PROVA', N'DESAFIO'));
GO
ALTER TABLE [dbo].[RecompensasConcedidas] CHECK CONSTRAINT [CK_RecompensasConcedidas_Tipo];
GO

-- RevisoesConteudo
ALTER TABLE [dbo].[RevisoesConteudo] WITH CHECK ADD CONSTRAINT [CK_RevisoesConteudo_Acao]
    CHECK ([Acao] IN (N'ENVIADO_REVISAO', N'APROVADO', N'REJEITADO', N'REMOVIDO'));
GO
ALTER TABLE [dbo].[RevisoesConteudo] CHECK CONSTRAINT [CK_RevisoesConteudo_Acao];
GO

ALTER TABLE [dbo].[RevisoesConteudo] WITH CHECK ADD CONSTRAINT [CK_RevisoesConteudo_Justificativa]
    CHECK (NOT ([Acao] IN (N'REMOVIDO', N'REJEITADO')) OR LEN([Justificativa]) > 0);
GO
ALTER TABLE [dbo].[RevisoesConteudo] CHECK CONSTRAINT [CK_RevisoesConteudo_Justificativa];
GO

ALTER TABLE [dbo].[RevisoesConteudo] WITH CHECK ADD CONSTRAINT [CK_RevisoesConteudo_Tipo]
    CHECK ([TipoConteudo] IN (N'TRILHA', N'MODULO', N'AULA', N'QUESTAO', N'SIMULADO'));
GO
ALTER TABLE [dbo].[RevisoesConteudo] CHECK CONSTRAINT [CK_RevisoesConteudo_Tipo];
GO

-- UsuarioTrilhas
ALTER TABLE [dbo].[UsuarioTrilhas] WITH CHECK ADD CONSTRAINT [CK_StatusUsuarioTrilha]
    CHECK ([Status] IN ('EM_ANDAMENTO', 'CONCLUIDA', 'CANCELADA'));
GO
ALTER TABLE [dbo].[UsuarioTrilhas] CHECK CONSTRAINT [CK_StatusUsuarioTrilha];
GO

ALTER TABLE [dbo].[UsuarioTrilhas] WITH CHECK ADD CONSTRAINT [CK_UsuarioTrilhas_Progresso_0_100]
    CHECK ([ProgressoPercentual] >= 0 AND [ProgressoPercentual] <= 100);
GO
ALTER TABLE [dbo].[UsuarioTrilhas] CHECK CONSTRAINT [CK_UsuarioTrilhas_Progresso_0_100];
GO

-- ============================================================================
-- 3. ÍNDICES DE PERFORMANCE E UNICIDADE
-- ============================================================================

-- AlternativasQuestao
CREATE NONCLUSTERED INDEX [IX_AlternativasQuestao_QuestaoID] ON [dbo].[AlternativasQuestao]
([QuestaoID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_AlternativasQuestao_UmaCorreta] ON [dbo].[AlternativasQuestao]
([QuestaoID] ASC) WHERE ([EhCorreta] = (1))
WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

-- Aulas
CREATE NONCLUSTERED INDEX [IX_Aulas_ModuloID] ON [dbo].[Aulas]
([ModuloID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_Aulas_ModuloID_IndiceOrdem] ON [dbo].[Aulas]
([ModuloID] ASC, [IndiceOrdem] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

-- ChatConversations & ChatMessages
CREATE NONCLUSTERED INDEX [IX_ChatConversations_Usuario_AtualizadoEm] ON [dbo].[ChatConversations]
([UsuarioID] ASC, [AtualizadoEm] DESC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_ChatMessages_Conversation_CreatedAt] ON [dbo].[ChatMessages]
([ConversationId] ASC, [CreatedAt] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_ChatMessages_Usuario_Conversation] ON [dbo].[ChatMessages]
([UsuarioID] ASC, [ConversationId] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

-- ConquistasUsuario & UsuarioConquistas
CREATE NONCLUSTERED INDEX [IX_ConquistasUsuario_ConquistaID] ON [dbo].[ConquistasUsuario]
([ConquistaID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_UsuarioConquistas_ConquistaID] ON [dbo].[UsuarioConquistas]
([ConquistaID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

-- HistoricoXP & XPUsuario
CREATE NONCLUSTERED INDEX [IX_HistoricoXP_UsuarioID] ON [dbo].[HistoricoXP]
([UsuarioID] ASC, [CriadoEm] DESC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_XPUsuario_NivelAtualID] ON [dbo].[XPUsuario]
([NivelAtualID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_XPUsuario_TotalXP] ON [dbo].[XPUsuario]
([TotalXP] DESC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

-- LogsAuditoria & RevisoesConteudo
CREATE NONCLUSTERED INDEX [IX_LogsAuditoria_CriadoEm] ON [dbo].[LogsAuditoria]
([CriadoEm] DESC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_RevisoesConteudo_Tipo_Conteudo_Data] ON [dbo].[RevisoesConteudo]
([TipoConteudo] ASC, [ConteudoID] ASC, [CriadoEm] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

-- Modulos
CREATE NONCLUSTERED INDEX [IX_Modulos_TrilhaID] ON [dbo].[Modulos]
([TrilhaID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_Modulos_TrilhaID_IndiceOrdem] ON [dbo].[Modulos]
([TrilhaID] ASC, [IndiceOrdem] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

-- ParticipacoesDesafio & RecompensasConcedidas
CREATE UNIQUE NONCLUSTERED INDEX [IX_ParticipacoesDesafio_Desafio_Usuario] ON [dbo].[ParticipacoesDesafio]
([DesafioID] ASC, [UsuarioID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_ParticipacoesDesafio_UsuarioID] ON [dbo].[ParticipacoesDesafio]
([UsuarioID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_RecompensasConcedidas_Usuario_Origem] ON [dbo].[RecompensasConcedidas]
([UsuarioID] ASC, [TipoOrigem] ASC, [OrigemID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

-- ProgressoUsuario
CREATE NONCLUSTERED INDEX [IX_ProgressoUsuario_Usuario_ConcluidoEm] ON [dbo].[ProgressoUsuario]
([UsuarioID] ASC, [ConcluidoEm] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

-- ProvaQuestoes, Provas, ProvasUsuario & RespostasProvasUsuario
CREATE NONCLUSTERED INDEX [IX_ProvaQuestoes_QuestaoID] ON [dbo].[ProvaQuestoes]
([QuestaoID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_Provas_Status_TrilhaID] ON [dbo].[Provas]
([Status] ASC, [TrilhaID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_ProvasUsuario_Usuario_Prova_Status] ON [dbo].[ProvasUsuario]
([UsuarioID] ASC, [ProvaID] ASC, [Status] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_ProvasUsuario_UsuarioID] ON [dbo].[ProvasUsuario]
([UsuarioID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_RespostasProvasUsuario_AlternativaQuestao] ON [dbo].[RespostasProvasUsuario]
([AlternativaSelecionadaID] ASC, [QuestaoID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_RespostasProvasUsuario_QuestaoID] ON [dbo].[RespostasProvasUsuario]
([QuestaoID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

-- Questoes
CREATE NONCLUSTERED INDEX [IX_Questoes_MateriaID] ON [dbo].[Questoes]
([MateriaID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_Questoes_ModuloID] ON [dbo].[Questoes]
([ModuloID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_Questoes_Status_NivelDificuldade] ON [dbo].[Questoes]
([Status] ASC, [NivelDificuldade] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_Questoes_TrilhaID] ON [dbo].[Questoes]
([TrilhaID] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

-- RefreshTokens & TokensRecuperacaoSenha
CREATE NONCLUSTERED INDEX [IX_RefreshTokens_Usuario_Family] ON [dbo].[RefreshTokens]
([UsuarioID] ASC, [TokenFamilyId] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_RefreshTokens_TokenHash] ON [dbo].[RefreshTokens]
([TokenHash] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_TokensRecuperacaoSenha_TokenHash] ON [dbo].[TokensRecuperacaoSenha]
([TokenHash] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO

CREATE NONCLUSTERED INDEX [IX_TokensRecuperacaoSenha_Usuario_CriadoEm] ON [dbo].[TokensRecuperacaoSenha]
([UsuarioID] ASC, [CriadoEm] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY];
GO