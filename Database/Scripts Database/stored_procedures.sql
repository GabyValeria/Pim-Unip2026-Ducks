--------------------------------------------------------------------------------
-- PROJETO DUCKS - PROCEDIMENTOS ARMAZENADOS (STORED PROCEDURES)
--------------------------------------------------------------------------------

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

--------------------------------------------------------------------------------
-- 1. AUTENTICAÇÃO DE UTILIZADOR
--------------------------------------------------------------------------------
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

    DECLARE @StoredHash VARBINARY(64);
    DECLARE @StoredSalt VARBINARY(32);
    DECLARE @EstaAtivo BIT;
    DECLARE @CalculatedHash VARBINARY(64);

    SELECT 
        @UsuarioID = u.UsuarioID,
        @StoredHash = u.SenhaHash,
        @EstaAtivo = u.EstaAtivo
    FROM dbo.Usuarios u
    WHERE u.NomeUsuario = @Identificador OR u.Email = @Identificador;

    IF @UsuarioID IS NULL OR @EstaAtivo = 0
    BEGIN
        INSERT INTO dbo.LogsAuditoria (UsuarioID, Acao, Detalhes, EnderecoIP)
        VALUES (NULL, 'LOGIN_FALHOU', CONCAT('Identificador não encontrado ou inativo: ', @Identificador), @EnderecoIP);
        RETURN;
    END

    SET @CalculatedHash = HASHBYTES('SHA2_512', CONVERT(VARBINARY(MAX), @SenhaPlainText) + @StoredSalt);

    IF @CalculatedHash = @StoredHash
    BEGIN
        SET @Sucesso = 1;
        INSERT INTO dbo.LogsAuditoria (UsuarioID, Acao, Detalhes, EnderecoIP)
        VALUES (@UsuarioID, 'LOGIN_SUCESSO', 'Autenticação realizada com sucesso.', @EnderecoIP);
    END
    ELSE
    BEGIN
        SET @Sucesso = 0;
        INSERT INTO dbo.LogsAuditoria (UsuarioID, Acao, Detalhes, EnderecoIP)
        VALUES (@UsuarioID, 'LOGIN_FALHOU', 'Senha incorreta informada.', @EnderecoIP);
        SET @UsuarioID = NULL;
    END
END;
GO

--------------------------------------------------------------------------------
-- 2. CRIAÇÃO DE SESSÃO DE PROVA ESPECÍFICA
--------------------------------------------------------------------------------
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
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH;
END;
GO

--------------------------------------------------------------------------------
-- 3. CRIAÇÃO DE SESSÃO DE PROVA GERAL
--------------------------------------------------------------------------------
CREATE PROCEDURE [dbo].[sp_CriarSessaoProvaGeral]
    @UsuarioID INT,
    @TotalQuestoesDesejadas INT = 10,
    @NovoProvaUsuarioID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    SET @NovoProvaUsuarioID = NULL;

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

--------------------------------------------------------------------------------
-- 4. SUBMISSÃO DE RESPOSTA E CÁLCULO DE XP
--------------------------------------------------------------------------------
CREATE PROCEDURE [dbo].[sp_SubmeterRespostaProva]
    @ProvaUsuarioID INT,
    @QuestaoID INT,
    @AlternativaSelecionadaID INT,
    @XPporQuestaoCorreta INT = 15
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

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

        SELECT @EhCorreta = EhCorreta
        FROM dbo.AlternativasQuestao
        WHERE AlternativaID = @AlternativaSelecionadaID AND QuestaoID = @QuestaoID;

        INSERT INTO dbo.RespostasProvasUsuario (ProvaUsuarioID, QuestaoID, AlternativaSelecionadaID, EhCorreta)
        VALUES (@ProvaUsuarioID, @QuestaoID, @AlternativaSelecionadaID, @EhCorreta);

        IF @EhCorreta = 1
        BEGIN
            SELECT @TotalXPAtual = TotalXP
            FROM dbo.XPUsuario WITH (UPDLOCK, HOLDLOCK)
            WHERE UsuarioID = @UsuarioID;

            SET @NovoTotalXP = @TotalXPAtual + @XPporQuestaoCorreta;
            
            SELECT TOP (1) @NivelAtualID = NivelID
            FROM dbo.NiveisGamificacao
            WHERE XPMinimo <= @NovoTotalXP
            ORDER BY XPMinimo DESC;

            UPDATE dbo.ProvasUsuario
            SET Pontuacao = Pontuacao + @XPporQuestaoCorreta
            WHERE ProvaUsuarioID = @ProvaUsuarioID;

            UPDATE dbo.XPUsuario
            SET TotalXP = @NovoTotalXP,
                NivelAtualID = @NivelAtualID,
                AtualizadoEm = SYSUTCDATETIME()
            WHERE UsuarioID = @UsuarioID;

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

--------------------------------------------------------------------------------
-- 5. MODERAÇÃO DE CONTEÚDO (TRILHAS, AULAS, QUESTÕES)
--------------------------------------------------------------------------------
CREATE PROCEDURE [dbo].[sp_AprovarConteudo]
    @UsuarioModeradorID INT,
    @TipoEntidade NVARCHAR(50),
    @EntidadeID INT,
    @NovoStatus NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

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
            UPDATE dbo.TrilhasAprendizagem SET Status = @NovoStatus, AprovadoPorUsuarioID = @UsuarioModeradorID, AtualizadoEm = SYSUTCDATETIME() WHERE TrilhaID = @EntidadeID;
        ELSE IF @TipoEntidade = N'AULA'
            UPDATE dbo.Aulas SET Status = @NovoStatus, AprovadoPorUsuarioID = @UsuarioModeradorID WHERE AulaID = @EntidadeID;
        ELSE
            UPDATE dbo.Questoes SET Status = @NovoStatus, AprovadoPorUsuarioID = @UsuarioModeradorID WHERE QuestaoID = @EntidadeID;

        INSERT INTO dbo.LogsAuditoria (UsuarioID, Acao, Detalhes)
        VALUES (@UsuarioModeradorID, N'MODERACAO_CONTEUDO', CONCAT(N'Tipo: ', @TipoEntidade, N' | ID: ', @EntidadeID, N' | Status: ', @NovoStatus));

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        ;THROW;
    END CATCH;
END;
GO

--------------------------------------------------------------------------------
-- 6. INSCRIÇÃO DE UTILIZADOR EM TRILHA
--------------------------------------------------------------------------------
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
            ;THROW 51140, N'Usuario nao encontrado ou inativo.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.TrilhasAprendizagem WITH (UPDLOCK, HOLDLOCK) WHERE TrilhaID = @TrilhaID AND Status = N'APROVADO')
            ;THROW 51141, N'A trilha nao existe ou nao esta aprovada para matricula.', 1;

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

--------------------------------------------------------------------------------
-- 7. CONCESSÃO AUTOMÁTICA DE CONQUISTAS
--------------------------------------------------------------------------------
CREATE PROCEDURE [dbo].[sp_VerificarEConcederConquistas]
    @UsuarioID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TotalXP INT = 0;
    DECLARE @ConquistaID INT;

    SELECT @TotalXP = ISNULL(TotalXP, 0) FROM dbo.XPUsuario WHERE UsuarioID = @UsuarioID;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Regra A: "Primeiro Passo" (>= 1 XP)
        IF @TotalXP >= 1
        BEGIN
            SELECT @ConquistaID = ConquistaID FROM dbo.Conquistas WHERE NomeConquista = N'Primeiro Passo';
            IF @ConquistaID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.ConquistasUsuario WHERE UsuarioID = @UsuarioID AND ConquistaID = @ConquistaID)
            BEGIN
                INSERT INTO dbo.ConquistasUsuario (UsuarioID, ConquistaID) VALUES (@UsuarioID, @ConquistaID);
                INSERT INTO dbo.LogsAuditoria (UsuarioID, Acao, Detalhes) VALUES (@UsuarioID, N'CONQUISTA_DESBLOQUEADA', CONCAT(N'Medalha: Primeiro Passo'));
            END
        END

        -- Regra B: "Explorador Focado" (>= 100 XP)
        IF @TotalXP >= 100
        BEGIN
            SELECT @ConquistaID = ConquistaID FROM dbo.Conquistas WHERE NomeConquista = N'Explorador Focado';
            IF @ConquistaID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.ConquistasUsuario WHERE UsuarioID = @UsuarioID AND ConquistaID = @ConquistaID)
            BEGIN
                INSERT INTO dbo.ConquistasUsuario (UsuarioID, ConquistaID) VALUES (@UsuarioID, @ConquistaID);
                INSERT INTO dbo.LogsAuditoria (UsuarioID, Acao, Detalhes) VALUES (@UsuarioID, N'CONQUISTA_DESBLOQUEADA', CONCAT(N'Medalha: Explorador Focado'));
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

--------------------------------------------------------------------------------
-- 8. RELATÓRIO GERENCIAL RESUMIDO
--------------------------------------------------------------------------------
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