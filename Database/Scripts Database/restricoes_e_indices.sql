--------------------------------------------------------------------------------
-- PROJETO DUCKS - RESTRIÇÕES (FK, CHECK, DEFAULT) E ÍNDICES
--------------------------------------------------------------------------------

-- VALORES PADRÃO (DEFAULT CONSTRAINTS)
ALTER TABLE [dbo].[Aulas] ADD CONSTRAINT [DF_Aulas_Status] DEFAULT ('PENDENTE') FOR [Status];
ALTER TABLE [dbo].[ConfiguracoesPlataforma] ADD CONSTRAINT [DF_Config_AtualizadoEm] DEFAULT (sysutcdatetime()) FOR [AtualizadoEm];
ALTER TABLE [dbo].[ConquistasUsuario] ADD CONSTRAINT [DF_ConquistasUsuario_ConquistadoEm] DEFAULT (sysutcdatetime()) FOR [ConquistadoEm];
ALTER TABLE [dbo].[HistoricoXP] ADD CONSTRAINT [DF_HistoricoXP_CriadoEm] DEFAULT (sysutcdatetime()) FOR [CriadoEm];
ALTER TABLE [dbo].[LogsAuditoria] ADD CONSTRAINT [DF_LogsAuditoria_CriadoEm] DEFAULT (sysutcdatetime()) FOR [CriadoEm];
ALTER TABLE [dbo].[Papeis] ADD CONSTRAINT [DF_Papeis_CriadoEm] DEFAULT (sysutcdatetime()) FOR [CriadoEm];
ALTER TABLE [dbo].[ProgressoUsuario] ADD CONSTRAINT [DF_ProgressoUsuario_ConcluidoEm] DEFAULT (sysutcdatetime()) FOR [ConcluidoEm];
ALTER TABLE [dbo].[Provas] ADD CONSTRAINT [DF_Provas_CriadoEm] DEFAULT (sysutcdatetime()) FOR [CriadoEm];
ALTER TABLE [dbo].[ProvasUsuario] ADD CONSTRAINT [DF_ProvasUsuario_IniciadoEm] DEFAULT (sysutcdatetime()) FOR [IniciadoEm];
ALTER TABLE [dbo].[ProvasUsuario] ADD CONSTRAINT [DF_ProvasUsuario_Pontuacao] DEFAULT ((0)) FOR [Pontuacao];
ALTER TABLE [dbo].[Questoes] ADD CONSTRAINT [DF_Questoes_Peso] DEFAULT ((10)) FOR [Peso];
ALTER TABLE [dbo].[Questoes] ADD CONSTRAINT [DF_Questoes_Status] DEFAULT ('PENDENTE') FOR [Status];
ALTER TABLE [dbo].[RespostasProvasUsuario] ADD CONSTRAINT [DF_RespostasProvas_RespondidoEm] DEFAULT (sysutcdatetime()) FOR [RespondidoEm];
ALTER TABLE [dbo].[TrilhasAprendizagem] ADD CONSTRAINT [DF_Trilhas_Status] DEFAULT ('PENDENTE') FOR [Status];
ALTER TABLE [dbo].[TrilhasAprendizagem] ADD CONSTRAINT [DF_Trilhas_AtualizadoEm] DEFAULT (sysutcdatetime()) FOR [AtualizadoEm];
ALTER TABLE [dbo].[UsuarioPapeis] ADD CONSTRAINT [DF_UsuarioPapeis_AtribuidoEm] DEFAULT (sysutcdatetime()) FOR [AtribuidoEm];
ALTER TABLE [dbo].[Usuarios] ADD CONSTRAINT [DF_Usuarios_EstaAtivo] DEFAULT ((1)) FOR [EstaAtivo];
ALTER TABLE [dbo].[Usuarios] ADD CONSTRAINT [DF_Usuarios_CriadoEm] DEFAULT (sysutcdatetime()) FOR [CriadoEm];
ALTER TABLE [dbo].[Usuarios] ADD CONSTRAINT [DF_Usuarios_AtualizadoEm] DEFAULT (sysutcdatetime()) FOR [AtualizadoEm];
ALTER TABLE [dbo].[UsuarioTrilhas] ADD CONSTRAINT [DF_UsuarioTrilhas_InscritoEm] DEFAULT (sysutcdatetime()) FOR [InscritoEm];
ALTER TABLE [dbo].[UsuarioTrilhas] ADD CONSTRAINT [DF_UsuarioTrilhas_Progresso] DEFAULT ((0.00)) FOR [ProgressoPercentual];
ALTER TABLE [dbo].[XPUsuario] ADD CONSTRAINT [DF_XPUsuario_Total] DEFAULT ((0)) FOR [TotalXP];
ALTER TABLE [dbo].[XPUsuario] ADD CONSTRAINT [DF_XPUsuario_AtualizadoEm] DEFAULT (sysutcdatetime()) FOR [AtualizadoEm];
GO

-- REGRAS DE VALIDAÇÃO (CHECK CONSTRAINTS)
ALTER TABLE [dbo].[Aulas] ADD CONSTRAINT [CK_StatusAula] CHECK ([Status]=N'REJEITADO' OR [Status]=N'APROVADO' OR [Status]=N'PENDENTE');
ALTER TABLE [dbo].[NiveisGamificacao] ADD CONSTRAINT [CK_NiveisGamificacao_Limites] CHECK ([NumeroNivel]>(0) AND [XPMinimo]>=(0));
ALTER TABLE [dbo].[Provas] ADD CONSTRAINT [CK_Provas_Tipo_Materia] CHECK (([TipoProva]=N'GERAL' AND [MateriaID] IS NULL OR [TipoProva]=N'ESPECIFICA' AND [MateriaID] IS NOT NULL));
ALTER TABLE [dbo].[Provas] ADD CONSTRAINT [CK_Provas_TotalQuestoes_Positivo] CHECK ([TotalQuestoes]>(0));
ALTER TABLE [dbo].[Provas] ADD CONSTRAINT [CK_TipoProva] CHECK ([TipoProva]='GERAL' OR [TipoProva]='ESPECIFICA');
ALTER TABLE [dbo].[ProvasUsuario] ADD CONSTRAINT [CK_StatusProvaUsuario] CHECK ([Status]='ABANDONADA' OR [Status]='CONCLUIDA' OR [Status]='EM_ANDAMENTO');
ALTER TABLE [dbo].[Questoes] ADD CONSTRAINT [CK_Dificuldade] CHECK ([NivelDificuldade]='DIFICIL' OR [NivelDificuldade]='MEDIO' OR [NivelDificuldade]='FACIL');
ALTER TABLE [dbo].[Questoes] ADD CONSTRAINT [CK_Questoes_Peso_Positivo] CHECK ([Peso]>(0));
ALTER TABLE [dbo].[Questoes] ADD CONSTRAINT [CK_TipoQuestao] CHECK ([TipoQuestao]='VERDADEIRO_FALSO' OR [TipoQuestao]='MULTIPLA_ESCOLHA');
ALTER TABLE [dbo].[TrilhasAprendizagem] ADD CONSTRAINT [CK_StatusTrilhaAprendizagem] CHECK ([Status]=N'REJEITADO' OR [Status]=N'APROVADO' OR [Status]=N'PENDENTE');
ALTER TABLE [dbo].[UsuarioTrilhas] ADD CONSTRAINT [CK_StatusUsuarioTrilha] CHECK ([Status]='CANCELADA' OR [Status]='CONCLUIDA' OR [Status]='EM_ANDAMENTO');
ALTER TABLE [dbo].[UsuarioTrilhas] ADD CONSTRAINT [CK_UsuarioTrilhas_Progresso_0_100] CHECK ([ProgressoPercentual]>=(0) AND [ProgressoPercentual]<=(100));
ALTER TABLE [dbo].[XPUsuario] ADD CONSTRAINT [CK_XPUsuario_TotalXP_NaoNegativo] CHECK ([TotalXP]>=(0));
GO

-- CHAVES ESTRANGEIRAS (FOREIGN KEYS)
ALTER TABLE [dbo].[AlternativasQuestao] ADD CONSTRAINT [FK_Alternativas_Questao] FOREIGN KEY([QuestaoID]) REFERENCES [dbo].[Questoes] ([QuestaoID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[Aulas] ADD CONSTRAINT [FK_Aulas_Aprovador] FOREIGN KEY([AprovadoPorUsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
ALTER TABLE [dbo].[Aulas] ADD CONSTRAINT [FK_Aulas_Criador] FOREIGN KEY([CriadoPorUsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
ALTER TABLE [dbo].[Aulas] ADD CONSTRAINT [FK_Aulas_Modulo] FOREIGN KEY([ModuloID]) REFERENCES [dbo].[Modulos] ([ModuloID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[ConfiguracoesPlataforma] ADD CONSTRAINT [FK_Config_Usuario] FOREIGN KEY([AtualizadoPorUsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE SET NULL;
ALTER TABLE [dbo].[ConquistasUsuario] ADD CONSTRAINT [FK_ConquistasUsuario_Conquista] FOREIGN KEY([ConquistaID]) REFERENCES [dbo].[Conquistas] ([ConquistaID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[ConquistasUsuario] ADD CONSTRAINT [FK_ConquistasUsuario_Usuario] FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[HistoricoXP] ADD CONSTRAINT [FK_HistoricoXP_Usuario] FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[LogsAuditoria] ADD CONSTRAINT [FK_LogsAuditoria_Usuario] FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE SET NULL;
ALTER TABLE [dbo].[Modulos] ADD CONSTRAINT [FK_Modulos_Trilha] FOREIGN KEY([TrilhaID]) REFERENCES [dbo].[TrilhasAprendizagem] ([TrilhaID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[ProgressoUsuario] ADD CONSTRAINT [FK_Progresso_Aula] FOREIGN KEY([AulaID]) REFERENCES [dbo].[Aulas] ([AulaID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[ProgressoUsuario] ADD CONSTRAINT [FK_Progresso_Usuario] FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[ProvaQuestoes] ADD CONSTRAINT [FK_ProvaQuestoes_Prova] FOREIGN KEY([ProvaID]) REFERENCES [dbo].[Provas] ([ProvaID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[ProvaQuestoes] ADD CONSTRAINT [FK_ProvaQuestoes_Questao] FOREIGN KEY([QuestaoID]) REFERENCES [dbo].[Questoes] ([QuestaoID]);
ALTER TABLE [dbo].[Provas] ADD CONSTRAINT [FK_Provas_Materia] FOREIGN KEY([MateriaID]) REFERENCES [dbo].[Materias] ([MateriaID]);
ALTER TABLE [dbo].[ProvasUsuario] ADD CONSTRAINT [FK_ProvasUsuario_Prova] FOREIGN KEY([ProvaID]) REFERENCES [dbo].[Provas] ([ProvaID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[ProvasUsuario] ADD CONSTRAINT [FK_ProvasUsuario_Usuario] FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[Questoes] ADD CONSTRAINT [FK_Questoes_Aprovador] FOREIGN KEY([AprovadoPorUsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
ALTER TABLE [dbo].[Questoes] ADD CONSTRAINT [FK_Questoes_Criador] FOREIGN KEY([CriadoPorUsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
ALTER TABLE [dbo].[Questoes] ADD CONSTRAINT [FK_Questoes_Materia] FOREIGN KEY([MateriaID]) REFERENCES [dbo].[Materias] ([MateriaID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[Questoes] ADD CONSTRAINT [FK_Questoes_Modulo] FOREIGN KEY([ModuloID]) REFERENCES [dbo].[Modulos] ([ModuloID]);
ALTER TABLE [dbo].[RespostasProvasUsuario] ADD CONSTRAINT [FK_Respostas_Alternativa_Questao] FOREIGN KEY([AlternativaSelecionadaID], [QuestaoID]) REFERENCES [dbo].[AlternativasQuestao] ([AlternativaID], [QuestaoID]);
ALTER TABLE [dbo].[RespostasProvasUsuario] ADD CONSTRAINT [FK_Respostas_ProvaUsuario] FOREIGN KEY([ProvaUsuarioID]) REFERENCES [dbo].[ProvasUsuario] ([ProvaUsuarioID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[RespostasProvasUsuario] ADD CONSTRAINT [FK_Respostas_Questao] FOREIGN KEY([QuestaoID]) REFERENCES [dbo].[Questoes] ([QuestaoID]);
ALTER TABLE [dbo].[TrilhasAprendizagem] ADD CONSTRAINT [FK_Trilhas_Aprovador] FOREIGN KEY([AprovadoPorUsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
ALTER TABLE [dbo].[TrilhasAprendizagem] ADD CONSTRAINT [FK_Trilhas_Criador] FOREIGN KEY([CriadoPorUsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]);
ALTER TABLE [dbo].[TrilhasAprendizagem] ADD CONSTRAINT [FK_Trilhas_Materia] FOREIGN KEY([MateriaID]) REFERENCES [dbo].[Materias] ([MateriaID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[UsuarioPapeis] ADD CONSTRAINT [FK_UsuarioPapeis_Papel] FOREIGN KEY([PapelID]) REFERENCES [dbo].[Papeis] ([PapelID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[UsuarioPapeis] ADD CONSTRAINT [FK_UsuarioPapeis_Usuario] FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[UsuarioTrilhas] ADD CONSTRAINT [FK_UsuarioTrilhas_Trilha] FOREIGN KEY([TrilhaID]) REFERENCES [dbo].[TrilhasAprendizagem] ([TrilhaID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[UsuarioTrilhas] ADD CONSTRAINT [FK_UsuarioTrilhas_Usuario] FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
ALTER TABLE [dbo].[XPUsuario] ADD CONSTRAINT [FK_XPUsuario_Nivel] FOREIGN KEY([NivelAtualID]) REFERENCES [dbo].[NiveisGamificacao] ([NivelID]);
ALTER TABLE [dbo].[XPUsuario] ADD CONSTRAINT [FK_XPUsuario_Usuario] FOREIGN KEY([UsuarioID]) REFERENCES [dbo].[Usuarios] ([UsuarioID]) ON DELETE CASCADE;
GO

-- ÍNDICES NÃO CLUSTERED E REGRAS DE INTEGRIDADE EXCLUSIVAS
CREATE NONCLUSTERED INDEX [IX_AlternativasQuestao_QuestaoID] ON [dbo].[AlternativasQuestao] ([QuestaoID] ASC);
CREATE UNIQUE NONCLUSTERED INDEX [UX_AlternativasQuestao_UmaCorreta] ON [dbo].[AlternativasQuestao] ([QuestaoID] ASC) WHERE ([EhCorreta]=(1));
CREATE NONCLUSTERED INDEX [IX_Aulas_ModuloID] ON [dbo].[Aulas] ([ModuloID] ASC);
CREATE NONCLUSTERED INDEX [IX_ConquistasUsuario_ConquistaID] ON [dbo].[ConquistasUsuario] ([ConquistaID] ASC);
CREATE NONCLUSTERED INDEX [IX_HistoricoXP_UsuarioID] ON [dbo].[HistoricoXP] ([UsuarioID] ASC, [CriadoEm] DESC);
CREATE NONCLUSTERED INDEX [IX_LogsAuditoria_CriadoEm] ON [dbo].[LogsAuditoria] ([CriadoEm] DESC);
CREATE NONCLUSTERED INDEX [IX_Modulos_TrilhaID] ON [dbo].[Modulos] ([TrilhaID] ASC);
CREATE NONCLUSTERED INDEX [IX_ProvaQuestoes_QuestaoID] ON [dbo].[ProvaQuestoes] ([QuestaoID] ASC);
CREATE NONCLUSTERED INDEX [IX_ProvasUsuario_UsuarioID] ON [dbo].[ProvasUsuario] ([UsuarioID] ASC);
CREATE NONCLUSTERED INDEX [IX_Questoes_MateriaID] ON [dbo].[Questoes] ([MateriaID] ASC);
CREATE NONCLUSTERED INDEX [IX_RespostasProvasUsuario_AlternativaQuestao] ON [dbo].[RespostasProvasUsuario] ([AlternativaSelecionadaID] ASC, [QuestaoID] ASC);
CREATE NONCLUSTERED INDEX [IX_RespostasProvasUsuario_QuestaoID] ON [dbo].[RespostasProvasUsuario] ([QuestaoID] ASC);
CREATE NONCLUSTERED INDEX [IX_XPUsuario_NivelAtualID] ON [dbo].[XPUsuario] ([NivelAtualID] ASC);
CREATE NONCLUSTERED INDEX [IX_XPUsuario_TotalXP] ON [dbo].[XPUsuario] ([TotalXP] DESC);
GO