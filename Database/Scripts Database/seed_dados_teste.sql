--------------------------------------------------------------------------------
-- PROJETO DUCKS - SCRIPT DE POPULAÇÃO DE DADOS INICIAIS (SEED)
-- Objetivo: Inserir dados fictícios para testes rápidos e validação do sistema.
--------------------------------------------------------------------------------

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Inserir Papéis do Sistema (RBAC)
INSERT INTO [dbo].[Papeis] ([NomePapel], [Descricao])
VALUES 
    (N'Administrador', N'Acesso total às configurações e gestão do sistema'),
    (N'Curador', N'Responsável pela criação e gestão de conteúdos didáticos e trilhas'),
    (N'Moderador', N'Responsável pela moderação e aprovação de conteúdos submetidos');
GO

-- 2. Inserir Níveis de Gamificação Iniciais
INSERT INTO [dbo].[NiveisGamificacao] ([NumeroNivel], [XPMinimo], [TituloNivel])
VALUES 
    (1, 0, N'Iniciante em Tecnologia'),
    (2, 100, N'Explorador Focado'),
    (3, 300, N'Desbravador Avançado');
GO

-- 3. Inserir Conquistas Padrão da Plataforma
INSERT INTO [dbo].[Conquistas] ([NomeConquista], [Descricao], [UrlIcone])
VALUES 
    (N'Primeiro Passo', N'Ganhou pelo menos 1 XP na plataforma', N'/icons/primeiro-passo.png'),
    (N'Explorador Focado', N'Acumulou 100 XP ou mais', N'/icons/explorador-focado.png'),
    (N'Desbravador', N'Concluiu pelo menos 1 Trilha de Aprendizagem', N'/icons/desbravador.png');
GO

-- 4. Inserir Matérias / Disciplinas de Exemplo
INSERT INTO [dbo].[Materias] ([NomeMateria], [Descricao])
VALUES 
    (N'Banco de Dados', N'Fundamentos de modelagem, SQL Server e otimização de consultas.'),
    (N'Arquitetura de Software', N'Padrões de projeto, microsserviços e boas práticas de desenvolvimento.');
GO

-- 5. Inserir Utilizador de Testes (Senha padrão fictícia com Hash/Salt seguros de exemplo)
-- Nota: O hash abaixo é um exemplo gerado para fins de teste acadêmico.
INSERT INTO [dbo].[Usuarios] ([NomeUsuario], [Email], [HashSenha], [SaltSenha], [EstaAtivo])
VALUES (
    N'admin_ducks', 
    N'admin@ducks.edu.br', 
    0x0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF, 
    0x0123456789ABCDEF0123456789ABCDEF, 
    1
);
GO

-- 6. Atribuir Papel de Administrador ao Utilizador de Testes
INSERT INTO [dbo].[UsuarioPapeis] ([UsuarioID], [PapelID])
SELECT u.UsuarioID, p.PapelID
FROM [dbo].[Usuarios] u
CROSS JOIN [dbo].[Papeis] p
WHERE u.NomeUsuario = N'admin_ducks' AND p.NomePapel = N'Administrador';
GO

-- 7. Inicializar o Saldo de XP para o Utilizador de Testes (Nível 1)
INSERT INTO [dbo].[XPUsuario] ([UsuarioID], [TotalXP], [NivelAtualID])
SELECT u.UsuarioID, 0, n.NivelID
FROM [dbo].[Usuarios] u
CROSS JOIN [dbo].[NiveisGamificacao] n
WHERE u.NomeUsuario = N'admin_ducks' AND n.NumeroNivel = 1;
GO

-- 8. Inserir Trilha de Aprendizagem de Exemplo (Já aprovada para testes)
INSERT INTO [dbo].[TrilhasAprendizagem] ([NomeTrilha], [Descricao], [MateriaID], [Status], [CriadoPorUsuarioID], [AprovadoPorUsuarioID])
SELECT 
    N'Jornada SQL Server Essentials', 
    N'Trilha completa para dominar o básico e intermediário de SQL Server.', 
    m.MateriaID, 
    N'APROVADO', 
    u.UsuarioID, 
    u.UsuarioID
FROM [dbo].[Materias] m
CROSS JOIN [dbo].[Usuarios] u
WHERE m.NomeMateria = N'Banco de Dados' AND u.NomeUsuario = N'admin_ducks';
GO