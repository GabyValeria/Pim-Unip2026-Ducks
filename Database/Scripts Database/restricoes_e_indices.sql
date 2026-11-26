-- ============================================================================
-- PROJETO DUCKS - BANCO DE DADOS [ducks-db]
-- Arquivo: procedimentos.sql
-- Descrição: Criação das 7 Stored Procedures do esquema [dbo] com controle
--            transacional (XACT_ABORT), validações de domínio e auditoria.
-- Ordem: Executado após restricoes_e_indices.sql.
-- ============================================================================

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- 1. AUTENTICAÇÃO E SEGURANÇA
-- ============================================================================

CREATE PROCEDURE [dbo].[sp_AutenticarUsuario]
    @Identificador NVARCHAR(255),
    @SenhaPlainText NVARCHAR(255),
    @EnderecoIP NVARCHAR(50) = NULL,
    @Sucesso BIT OUTPUT,
    @UsuarioID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Sucesso = 0;
    SET @UsuarioID = NULL;

    DECLARE @InputHash VARCHAR(64);
    SET @InputHash = CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', @SenhaPlainText), 2);

    SELECT TOP 1 @UsuarioID = u.UsuarioID
    FROM dbo.Usuarios u
    WHERE (u.Email = @Identificador OR u.NomeUsuario = @Identificador)
      AND u.EstaAtivo = 1
      AND u.SenhaHash = @InputHash;

    IF @UsuarioID IS NOT NULL
    BEGIN
        SET @Sucesso = 1;
        INSERT INTO dbo.LogsAuditoria (UsuarioID, Acao, Detalhes, EnderecoIP)
        VALUES (@UsuarioID, N'LOGIN_SUCESSO', N'Autenticação realizada com sucesso.', @EnderecoIP);
    END
    ELSE
    BEGIN
        SET @Sucesso = 0;
        SET @UsuarioID = NULL;
        INSERT INTO dbo.LogsAuditoria (UsuarioID, Acao, Detalhes, EnderecoIP)
        VALUES (NULL, N'LOGIN_FALHOU', CONCAT(N'Tentativa de login falha para: ', @Identificador), @EnderecoIP);
    END
END;
GO

-- ============================================================================
-- 2. CURADORIA E MODERAÇÃO DE CONTEÚDO
-- ============================================================================

CREATE PROCEDURE [dbo].[sp_AprovarConteudo]
    @UsuarioModeradorID INT,
    @TipoEntidade NVARCHAR(50),
    @EntidadeID INT,
    @NovoStatus NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @TipoEntidade IS NULL OR @TipoEntidade NOT IN (N'TRILHA', N'AULA', N'QUESTAO')
    BEGIN
        ;THROW 51130, N'Tipo de entidade invalido. Use TRILHA, AULA ou QUESTAO.', 1;
    END;
    IF @EntidadeID IS NULL OR @EntidadeID <= 0
    BEGIN
        ;THROW 51131, N'EntidadeID invalido.', 1;
    END;
    IF @NovoStatus IS NULL OR @NovoStatus NOT IN (N'APROVADO', N'REJEITADO', N'PENDENTE')
    BEGIN
        ;THROW 51132, N'Status invalido. Use APROVADO, REJEITADO ou PENDENTE.', 1;
    END;
    IF NOT EXISTS (
        SELECT 1
        FROM dbo.Usuarios u
        JOIN dbo.UsuarioPapeis up ON up.UsuarioID = u.UsuarioID
        JOIN dbo.Papeis p ON p.PapelID = up.PapelID
        WHERE u.UsuarioID = @UsuarioModeradorID
          AND u.EstaAtivo = 1
          AND p.NomePapel IN (N'Moderador', N'Administrador')
    )
    BEGIN
        ;THROW 51133, N'O usuario precisa estar ativo e possuir o papel Moderador ou Administrador.', 1;
    END;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF @TipoEntidade = N'TRILHA'
        BEGIN
            UPDATE dbo.TrilhasAprendizagem
            SET Status = @NovoStatus, AprovadoPorUsuarioID = @UsuarioModeradorID, AtualizadoEm = SYSUTCDATETIME()
            WHERE TrilhaID = @EntidadeID;
            IF @@ROWCOUNT <> 1
            BEGIN
                ;THROW 51134, N'Trilha nao encontrada.', 1;
            END;
        END
        ELSE IF @TipoEntidade = N'AULA'
        BEGIN
            UPDATE dbo.Aulas
            SET Status = @NovoStatus, AprovadoPorUsuarioID = @UsuarioModeradorID
            WHERE AulaID = @EntidadeID;
            IF @@ROWCOUNT <> 1
            BEGIN
                ;THROW 51135, N'Aula nao encontrada.', 1;
            END;
        END
        ELSE
        BEGIN
            UPDATE dbo.Questoes
            SET Status = @NovoStatus, AprovadoPorUsuarioID = @UsuarioModeradorID
            WHERE QuestaoID = @EntidadeID;
            IF @@ROWCOUNT <> 1
            BEGIN
                ;THROW 51136, N'Questao nao encontrada.', 1;
            END;
        END;

        INSERT INTO dbo.LogsAuditoria (UsuarioID, Acao, Detalhes)
        VALUES (@UsuarioModeradorID, N'MODERACAO_CONTEUDO',
                CONCAT(N'Tipo: ', @TipoEntidade, N' | ID: ', @EntidadeID, N' | Status: ', @NovoStatus));

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH;
END;
GO

-- ============================================================================
-- 3. TRILHAS DE APRENDIZAGEM E MATRÍCULAS
-- ============================================================================

CREATE PROCEDURE [dbo].[sp_InscreverUsuarioTrilha]
    @UsuarioID INT,
    @TrilhaID INT,
    @UsuarioTrilhaID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @UsuarioTrilhaID = NULL;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WITH (UPDLOCK, HOLDLOCK) WHERE UsuarioID = @UsuarioID AND EstaAtivo = 1)
        BEGIN
            ;THROW 51140, N'Usuario nao encontrado ou inativo.', 1;
        END;
        IF NOT EXISTS (SELECT 1 FROM dbo.TrilhasAprendizagem WITH (UPDLOCK, HOLDLOCK) WHERE TrilhaID = @TrilhaID AND Status = N'APROVADO')
        BEGIN
            ;THROW 51141, N'A trilha nao existe ou nao esta aprovada para matricula.', 1;
        END;

        SELECT @UsuarioTrilhaID = UsuarioTrilhaID
        FROM dbo.UsuarioTrilhas WITH (UPDLOCK, HOLDLOCK)
        WHERE UsuarioID = @UsuarioID AND TrilhaID = @TrilhaID;

        IF @UsuarioTrilhaID IS NOT NULL
        BEGIN
            COMMIT TRANSACTION;
            RETURN;
        END;

        INSERT INTO dbo.UsuarioTrilhas (UsuarioID, TrilhaID, Status, ProgressoPercentual)
        VALUES (@UsuarioID, @TrilhaID, N'EM_ANDAMENTO', 0.00);
        SET @UsuarioTrilhaID = CONVERT(INT, SCOPE_IDENTITY());

        INSERT INTO dbo.LogsAuditoria (UsuarioID, Acao, Detalhes)
        VALUES (@UsuarioID, N'INSCRICAO_TRILHA', CONCAT(N'TrilhaID: ', @TrilhaID));

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH;
END;
GO

-- ============================================================================
-- 4. AVALIAÇÕES E SIMULADOS (CRIAÇÃO DE SESSÕES E SUBMISSÃO DE RESPOSTAS)
-- ============================================================================

CREATE PROCEDURE [dbo].[sp_CriarSessaoProvaEspecifica]
    @UsuarioID INT,
    @MateriaID INT,
    @TotalQuestoesDesejadas INT = 10,
    @NovoProvaUsuarioID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @NovoProvaUsuarioID = NULL;

    IF @TotalQuestoesDesejadas IS NULL OR @TotalQuestoesDesejadas <= 0
    BEGIN
        ;THROW 51100, N'A quantidade de questoes deve ser maior que zero.', 1;
    END;
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE UsuarioID = @UsuarioID AND EstaAtivo = 1)
    BEGIN
        ;THROW 51101, N'Usuario nao encontrado ou inativo.', 1;
    END;
    IF NOT EXISTS (SELECT 1 FROM dbo.Materias WHERE MateriaID = @MateriaID)
    BEGIN
        ;THROW 51102, N'Materia nao encontrada.', 1;
    END;
    IF (SELECT COUNT(*) FROM dbo.Questoes q
        WHERE q.MateriaID = @MateriaID AND q.Status = N'APROVADO'
          AND EXISTS (SELECT 1 FROM dbo.AlternativasQuestao a WHERE a.QuestaoID = q.QuestaoID AND a.EhCorreta = 1)) < @TotalQuestoesDesejadas
    BEGIN
        ;THROW 51103, N'Nao existem questoes aprovadas e respondiveis suficientes para esta materia.', 1;
    END;

    DECLARE @Questoes TABLE (QuestaoID INT NOT NULL PRIMARY KEY, OrdemQuestao INT NOT NULL UNIQUE);
    DECLARE @ProvaID INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO @Questoes (QuestaoID, OrdemQuestao)
        SELECT s.QuestaoID, ROW_NUMBER() OVER (ORDER BY NEWID())
        FROM (
            SELECT TOP (@TotalQuestoesDesejadas) q.QuestaoID
            FROM dbo.Questoes q
            WHERE q.MateriaID = @MateriaID
              AND q.Status = N'APROVADO'
              AND EXISTS (SELECT 1 FROM dbo.AlternativasQuestao a WHERE a.QuestaoID = q.QuestaoID AND a.EhCorreta = 1)
            ORDER BY NEWID()
        ) AS s;

        IF (SELECT COUNT(*) FROM @Questoes) <> @TotalQuestoesDesejadas
        BEGIN
            ;THROW 51104, N'O conjunto de questoes mudou durante a criacao da prova. Tente novamente.', 1;
        END;

        INSERT INTO dbo.Provas (Titulo, TipoProva, MateriaID, TotalQuestoes)
        SELECT N'Avaliacao de ' + NomeMateria, N'ESPECIFICA', MateriaID, @TotalQuestoesDesejadas
        FROM dbo.Materias WHERE MateriaID = @MateriaID;
        SET @ProvaID = CONVERT(INT, SCOPE_IDENTITY());

        INSERT INTO dbo.ProvaQuestoes (ProvaID, QuestaoID, OrdemQuestao)
        SELECT @ProvaID, QuestaoID, OrdemQuestao FROM @Questoes;

        INSERT INTO dbo.ProvasUsuario (UsuarioID, ProvaID, IniciadoEm, Status, Pontuacao)
        VALUES (@UsuarioID, @ProvaID, SYSUTCDATETIME(), N'EM_ANDAMENTO', 0);
        SET @NovoProvaUsuarioID = CONVERT(INT, SCOPE_IDENTITY());

        COMMIT TRANSACTION;
        SELECT @NovoProvaUsuarioID AS ProvaUsuarioID, @ProvaID AS ProvaID,
               @MateriaID AS MateriaID, @TotalQuestoesDesejadas AS TotalQuestoes;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE [dbo].[sp_CriarSessaoProvaGeral]
    @UsuarioID INT,
    @TotalQuestoesDesejadas INT = 10,
    @NovoProvaUsuarioID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @NovoProvaUsuarioID = NULL;

    IF @TotalQuestoesDesejadas IS NULL OR @TotalQuestoesDesejadas <= 0
    BEGIN
        ;THROW 51110, N'A quantidade de questoes deve ser maior que zero.', 1;
    END;
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE UsuarioID = @UsuarioID AND EstaAtivo = 1)
    BEGIN
        ;THROW 51111, N'Usuario nao encontrado ou inativo.', 1;
    END;
    IF (SELECT COUNT(*) FROM dbo.Questoes q
        WHERE q.Status = N'APROVADO'
          AND EXISTS (SELECT 1 FROM dbo.AlternativasQuestao a WHERE a.QuestaoID = q.QuestaoID AND a.EhCorreta = 1)) < @TotalQuestoesDesejadas
    BEGIN
        ;THROW 51112, N'Nao existem questoes aprovadas e respondiveis suficientes para a prova geral.', 1;
    END;

    DECLARE @Questoes TABLE (QuestaoID INT NOT NULL PRIMARY KEY, OrdemQuestao INT NOT NULL UNIQUE);
    DECLARE @ProvaID INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        ;WITH Candidatas AS
        (
            SELECT q.QuestaoID,
                   ROW_NUMBER() OVER (PARTITION BY q.MateriaID ORDER BY NEWID()) AS OrdemDentroDaMateria
            FROM dbo.Questoes q
            WHERE q.Status = N'APROVADO'
              AND EXISTS (SELECT 1 FROM dbo.AlternativasQuestao a WHERE a.QuestaoID = q.QuestaoID AND a.EhCorreta = 1)
        ), Selecionadas AS
        (
            SELECT TOP (@TotalQuestoesDesejadas) QuestaoID, OrdemDentroDaMateria
            FROM Candidatas
            ORDER BY CASE WHEN OrdemDentroDaMateria = 1 THEN 0 ELSE 1 END, NEWID()
        )
        INSERT INTO @Questoes (QuestaoID, OrdemQuestao)
        SELECT QuestaoID, ROW_NUMBER() OVER (ORDER BY NEWID())
        FROM Selecionadas;

        IF (SELECT COUNT(*) FROM @Questoes) <> @TotalQuestoesDesejadas
        BEGIN
            ;THROW 51113, N'O conjunto de questoes mudou durante a criacao da prova. Tente novamente.', 1;
        END;

        INSERT INTO dbo.Provas (Titulo, TipoProva, MateriaID, TotalQuestoes)
        VALUES (N'Simulado Geral de Tecnologia', N'GERAL', NULL, @TotalQuestoesDesejadas);
        SET @ProvaID = CONVERT(INT, SCOPE_IDENTITY());

        INSERT INTO dbo.ProvaQuestoes (ProvaID, QuestaoID, OrdemQuestao)
        SELECT @ProvaID, QuestaoID, OrdemQuestao FROM @Questoes;

        INSERT INTO dbo.ProvasUsuario (UsuarioID, ProvaID, IniciadoEm, Status, Pontuacao)
        VALUES (@UsuarioID, @ProvaID, SYSUTCDATETIME(), N'EM_ANDAMENTO', 0);
        SET @NovoProvaUsuarioID = CONVERT(INT, SCOPE_IDENTITY());

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE [dbo].[sp_SubmeterRespostaProva]
    @ProvaUsuarioID INT,
    @QuestaoID INT,
    @AlternativaSelecionadaID INT,
    @XPporQuestaoCorreta INT = 15
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @XPporQuestaoCorreta IS NULL OR @XPporQuestaoCorreta < 0
    BEGIN
        ;THROW 51120, N'O XP por resposta correta nao pode ser nulo ou negativo.', 1;
    END;

    DECLARE @StatusProva NVARCHAR(30), @UsuarioID INT, @ProvaID INT;
    DECLARE @EhCorreta BIT, @TotalXPAtual INT, @NovoTotalXP INT, @NivelAtualID INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @StatusProva = Status, @UsuarioID = UsuarioID, @ProvaID = ProvaID
        FROM dbo.ProvasUsuario WITH (UPDLOCK, HOLDLOCK)
        WHERE ProvaUsuarioID = @ProvaUsuarioID;

        IF @StatusProva IS NULL OR @StatusProva <> N'EM_ANDAMENTO'
        BEGIN
            ;THROW 51121, N'A prova informada nao esta ativa ou nao foi encontrada.', 1;
        END;

        IF NOT EXISTS (SELECT 1 FROM dbo.ProvaQuestoes WHERE ProvaID = @ProvaID AND QuestaoID = @QuestaoID)
        BEGIN
            ;THROW 51122, N'A questao nao pertence a esta prova.', 1;
        END;

        IF EXISTS (SELECT 1 FROM dbo.RespostasProvasUsuario WHERE ProvaUsuarioID = @ProvaUsuarioID AND QuestaoID = @QuestaoID)
        BEGIN
            ;THROW 51123, N'Esta questao ja foi respondida nesta tentativa.', 1;
        END;

        SELECT @EhCorreta = EhCorreta
        FROM dbo.AlternativasQuestao
        WHERE AlternativaID = @AlternativaSelecionadaID AND QuestaoID = @QuestaoID;

        IF @EhCorreta IS NULL
        BEGIN
            ;THROW 51124, N'A alternativa selecionada nao pertence a questao informada.', 1;
        END;

        INSERT INTO dbo.RespostasProvasUsuario (ProvaUsuarioID, QuestaoID, AlternativaSelecionadaID, EhCorreta)
        VALUES (@ProvaUsuarioID, @QuestaoID, @AlternativaSelecionadaID, @EhCorreta);

        IF @EhCorreta = 1
        BEGIN
            SELECT @TotalXPAtual = TotalXP
            FROM dbo.XPUsuario WITH (UPDLOCK, HOLDLOCK)
            WHERE UsuarioID = @UsuarioID;

            IF @TotalXPAtual IS NULL
            BEGIN
                ;THROW 51125, N'XPUsuario nao foi inicializado para este usuario. Inicialize XP e nivel antes de submeter respostas.', 1;
            END;

            SET @NovoTotalXP = @TotalXPAtual + @XPporQuestaoCorreta;
            SET @NivelAtualID = NULL;
            SELECT TOP (1) @NivelAtualID = NivelID
            FROM dbo.NiveisGamificacao
            WHERE XPMinimo <= @NovoTotalXP
            ORDER BY XPMinimo DESC;

            IF @NivelAtualID IS NULL
            BEGIN
                ;THROW 51126, N'Nao existe nivel de gamificacao aplicavel ao XP atual. Configure um nivel inicial com XPMinimo adequado.', 1;
            END;

            UPDATE dbo.ProvasUsuario
            SET Pontuacao = Pontuacao + @XPporQuestaoCorreta
            WHERE ProvaUsuarioID = @ProvaUsuarioID;

            UPDATE dbo.XPUsuario
            SET TotalXP = @NovoTotalXP,
                NivelAtualID = @NivelAtualID,
                AtualizadoEm = SYSUTCDATETIME()
            WHERE UsuarioID = @UsuarioID;

            IF @@ROWCOUNT <> 1
            BEGIN
                ;THROW 51127, N'Falha ao atualizar o saldo de XP do usuario.', 1;
            END;

            INSERT INTO dbo.HistoricoXP (UsuarioID, QuantidadeXP, Motivo, ReferenciaID)
            VALUES (@UsuarioID, @XPporQuestaoCorreta, N'RESPOSTA_CORRETA_PROVA', @ProvaUsuarioID);
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH;
END;
GO

-- ============================================================================
-- 5. GAMIFICAÇÃO E RELATÓRIOS GERENCIAIS
-- ============================================================================

CREATE PROCEDURE [dbo].[sp_VerificarEConcederConquistas]
    @UsuarioID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TotalXP INT = 0;
    DECLARE @ConquistaID INT;

    SELECT @TotalXP = ISNULL(TotalXP, 0)
    FROM dbo.XPUsuario
    WHERE UsuarioID = @UsuarioID;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Regra A: "Primeiro Passo" (Ganhou pelo menos 1 XP)
        IF @TotalXP >= 1
        BEGIN
            SELECT @ConquistaID = ConquistaID FROM dbo.Conquistas WHERE NomeConquista = N'Primeiro Passo';

            IF @ConquistaID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.ConquistasUsuario WHERE UsuarioID = @UsuarioID AND ConquistaID = @ConquistaID)
            BEGIN
                INSERT INTO dbo.ConquistasUsuario (UsuarioID, ConquistaID) VALUES (@UsuarioID, @ConquistaID);

                INSERT INTO dbo.LogsAuditoria (UsuarioID, Acao, Detalhes)
                VALUES (@UsuarioID, N'CONQUISTA_DESBLOQUEADA', CONCAT(N'Medalha conquistada: Primeiro Passo (ID: ', @ConquistaID, N')'));
            END
        END

        -- Regra B: "Explorador Focado" (Acumulou 100 XP ou mais)
        IF @TotalXP >= 100
        BEGIN
            SELECT @ConquistaID = ConquistaID FROM dbo.Conquistas WHERE NomeConquista = N'Explorador Focado';

            IF @ConquistaID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.ConquistasUsuario WHERE UsuarioID = @UsuarioID AND ConquistaID = @ConquistaID)
            BEGIN
                INSERT INTO dbo.ConquistasUsuario (UsuarioID, ConquistaID) VALUES (@UsuarioID, @ConquistaID);

                INSERT INTO dbo.LogsAuditoria (UsuarioID, Acao, Detalhes)
                VALUES (@UsuarioID, N'CONQUISTA_DESBLOQUEADA', CONCAT(N'Medalha conquistada: Explorador Focado (ID: ', @ConquistaID, N')'));
            END
        END

        -- Regra C: "Desbravador" (Concluiu pelo menos 1 Trilha de Aprendizagem)
        IF EXISTS (SELECT 1 FROM dbo.UsuarioTrilhas WHERE UsuarioID = @UsuarioID AND (Status = N'CONCLUIDA' OR ProgressoPercentual >= 100.00))
        BEGIN
            SELECT @ConquistaID = ConquistaID FROM dbo.Conquistas WHERE NomeConquista = N'Desbravador';

            IF @ConquistaID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.ConquistasUsuario WHERE UsuarioID = @UsuarioID AND ConquistaID = @ConquistaID)
            BEGIN
                INSERT INTO dbo.ConquistasUsuario (UsuarioID, ConquistaID) VALUES (@UsuarioID, @ConquistaID);

                INSERT INTO dbo.LogsAuditoria (UsuarioID, Acao, Detalhes)
                VALUES (@UsuarioID, N'CONQUISTA_DESBLOQUEADA', CONCAT(N'Medalha conquistada: Desbravador (ID: ', @ConquistaID, N')'));
            END
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH;
END;
GO

CREATE PROCEDURE [dbo].[sp_RelatorioGerencialResumo]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) FROM dbo.Usuarios WHERE EstaAtivo = 1) AS TotalUsuariosAtivos,
        (SELECT COUNT(*) FROM dbo.TrilhasAprendizagem) AS TotalTrilhas,
        (SELECT COUNT(*) FROM dbo.TrilhasAprendizagem WHERE Status = N'PENDENTE') AS TrilhasPendentesModeracao,
        (SELECT COUNT(*) FROM dbo.Aulas) AS TotalAulas,
        (SELECT COUNT(*) FROM dbo.Questoes) AS TotalQuestoes,
        (SELECT COUNT(*) FROM dbo.Questoes WHERE Status = N'PENDENTE') AS QuestoesPendentesModeracao,
        (SELECT ISNULL(SUM(TotalXP), 0) FROM dbo.XPUsuario) AS TotalXPDistribuidoPlataforma;

    SELECT Status, COUNT(*) AS TotalMatriculas
    FROM dbo.UsuarioTrilhas
    GROUP BY Status;
END;
GO