--------------------------------------------------------------------------------
-- PROJETO DUCKS - SCRIPT DE INFRAESTRUTURA E CONFIGURAÇÃO DO BANCO DE DADOS
--------------------------------------------------------------------------------

-- Criação do Banco de Dados no Azure SQL Database
CREATE DATABASE [ducks-db]  
(EDITION = 'GeneralPurpose', SERVICE_OBJECTIVE = 'GP_S_Gen5_2', MAXSIZE = 32 GB) 
WITH CATALOG_COLLATION = SQL_Latin1_General_CP1_CI_AS, LEDGER = OFF;
GO

ALTER DATABASE [ducks-db] SET COMPATIBILITY_LEVEL = 170;
GO

-- Configurações de ANSI e Otimização do SGBD
ALTER DATABASE [ducks-db] SET ANSI_NULL_DEFAULT ON;
ALTER DATABASE [ducks-db] SET ANSI_NULLS ON;
ALTER DATABASE [ducks-db] SET ANSI_PADDING ON;
ALTER DATABASE [ducks-db] SET ANSI_WARNINGS ON;
ALTER DATABASE [ducks-db] SET ARITHABORT ON;
ALTER DATABASE [ducks-db] SET AUTO_SHRINK OFF;
ALTER DATABASE [ducks-db] SET AUTO_UPDATE_STATISTICS ON;
ALTER DATABASE [ducks-db] SET CURSOR_CLOSE_ON_COMMIT OFF;
ALTER DATABASE [ducks-db] SET CONCAT_NULL_YIELDS_NULL ON;
ALTER DATABASE [ducks-db] SET NUMERIC_ROUNDABORT OFF;
ALTER DATABASE [ducks-db] SET QUOTED_IDENTIFIER ON;
ALTER DATABASE [ducks-db] SET RECURSIVE_TRIGGERS OFF;
ALTER DATABASE [ducks-db] SET AUTO_UPDATE_STATISTICS_ASYNC OFF;
ALTER DATABASE [ducks-db] SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE [ducks-db] SET PARAMETERIZATION SIMPLE;
ALTER DATABASE [ducks-db] SET READ_COMMITTED_SNAPSHOT ON;
ALTER DATABASE [ducks-db] SET MULTI_USER;
ALTER DATABASE [ducks-db] SET AUTOMATIC_INDEX_COMPACTION = OFF;
ALTER DATABASE [ducks-db] SET ENCRYPTION ON;
GO

-- Habilitação do Query Store para monitoramento de performance
ALTER DATABASE [ducks-db] SET QUERY_STORE = ON;
ALTER DATABASE [ducks-db] SET QUERY_STORE 
(
    OPERATION_MODE = READ_WRITE, 
    CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), 
    DATA_FLUSH_INTERVAL_SECONDS = 900, 
    INTERVAL_LENGTH_MINUTES = 60, 
    MAX_STORAGE_SIZE_MB = 100, 
    QUERY_CAPTURE_MODE = AUTO, 
    SIZE_BASED_CLEANUP_MODE = AUTO, 
    MAX_PLANS_PER_QUERY = 200, 
    WAIT_STATS_CAPTURE_MODE = ON
);
GO

--------------------------------------------------------------------------------
-- ROLES DE SEGURANÇA E PAPÉIS DE BANCO DE DADOS (RBAC)
--------------------------------------------------------------------------------

-- Criação das Roles internas para controle de acesso segregado
CREATE ROLE [Role_Ducks_Moderacao];
CREATE ROLE [Role_Ducks_Curadoria];
CREATE ROLE [Role_Ducks_Administracao];
GO

-- Nota de Segurança: Usuários lógicos (CREATE USER) com senhas em texto plano 
-- devem ser gerenciados via variáveis de ambiente ou secrets no pipeline de CI/CD.
-- Exemplo de criação de estrutura de usuários lógicos mapeados às roles:
-- CREATE USER [DbUser_Moderador] WITH PASSWORD=N'SUA_SENHA_FORTE_AQUI', DEFAULT_SCHEMA=[dbo];
-- sys.sp_addrolemember @rolename = N'Role_Ducks_Moderacao', @membername = N'DbUser_Moderador';
-- GO