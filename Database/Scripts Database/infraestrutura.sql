-- ============================================================================
-- PROJETO DUCKS - BANCO DE DADOS [ducks-db]
-- Arquivo: infraestrutura.sql
-- Descrição: Criação do banco de dados, configurações de escopo, usuários,
--            roles (papéis de segurança) e associações de permissão.
-- ============================================================================

-- 1. CRIAÇÃO DO BANCO DE DADOS (AZURE SQL SERVERLESS)
CREATE DATABASE [ducks-db]
    (EDITION = 'GeneralPurpose', SERVICE_OBJECTIVE = 'GP_S_Gen5_2', MAXSIZE = 32 GB)
    WITH CATALOG_COLLATION = SQL_Latin1_General_CP1_CI_AS, LEDGER = OFF;
GO

ALTER DATABASE [ducks-db] SET COMPATIBILITY_LEVEL = 170;
GO
ALTER DATABASE [ducks-db] SET ANSI_NULL_DEFAULT ON;
GO
ALTER DATABASE [ducks-db] SET ANSI_NULLS ON;
GO
ALTER DATABASE [ducks-db] SET ANSI_PADDING ON;
GO
ALTER DATABASE [ducks-db] SET ANSI_WARNINGS ON;
GO
ALTER DATABASE [ducks-db] SET ARITHABORT ON;
GO
ALTER DATABASE [ducks-db] SET AUTO_SHRINK OFF;
GO
ALTER DATABASE [ducks-db] SET AUTO_UPDATE_STATISTICS ON;
GO
ALTER DATABASE [ducks-db] SET CURSOR_CLOSE_ON_COMMIT OFF;
GO
ALTER DATABASE [ducks-db] SET CONCAT_NULL_YIELDS_NULL ON;
GO
ALTER DATABASE [ducks-db] SET NUMERIC_ROUNDABORT OFF;
GO
ALTER DATABASE [ducks-db] SET QUOTED_IDENTIFIER ON;
GO
ALTER DATABASE [ducks-db] SET RECURSIVE_TRIGGERS OFF;
GO
ALTER DATABASE [ducks-db] SET AUTO_UPDATE_STATISTICS_ASYNC OFF;
GO
ALTER DATABASE [ducks-db] SET ALLOW_SNAPSHOT_ISOLATION ON;
GO
ALTER DATABASE [ducks-db] SET PARAMETERIZATION SIMPLE;
GO
ALTER DATABASE [ducks-db] SET READ_COMMITTED_SNAPSHOT ON;
GO
ALTER DATABASE [ducks-db] SET MULTI_USER;
GO
ALTER DATABASE [ducks-db] SET AUTOMATIC_INDEX_COMPACTION = OFF;
GO
ALTER DATABASE [ducks-db] SET ENCRYPTION ON;
GO
ALTER DATABASE [ducks-db] SET QUERY_STORE = ON;
GO
ALTER DATABASE [ducks-db] SET QUERY_STORE (
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
ALTER DATABASE [ducks-db] SET READ_WRITE;
GO

-- ============================================================================
-- NOTA: Os comandos abaixo devem ser executados conectados ao banco [ducks-db]
-- ============================================================================

-- 2. CRIAÇÃO DE USUÁRIOS CONTIDOS (CONTAINED USERS)
-- Nota de Segurança: Substitua as senhas em ambiente de produção via Azure Key Vault / Secrets.
CREATE USER [DbUser_Moderador] WITH PASSWORD = N'WfyjFcU50oqonuQeD5hMase/xakRdxzEri5SJX50DTM=', DEFAULT_SCHEMA = [dbo];
GO

CREATE USER [DbUser_Curador] WITH PASSWORD = N'K6slB63uz4G9sHU5uHmHp06Oy5wRJS1Yl5E1/5O7nyQ=', DEFAULT_SCHEMA = [dbo];
GO

CREATE USER [DbUser_Admin] WITH PASSWORD = N'41hBl+7cNEac9+aNCstwBoD5g85xIu3n1OufD+LOsbE=', DEFAULT_SCHEMA = [dbo];
GO

-- 3. CRIAÇÃO DE ROLES (PAPÉIS DE BANCO DE DADOS)
CREATE ROLE [Role_Ducks_Moderacao];
GO

CREATE ROLE [Role_Ducks_Curadoria];
GO

CREATE ROLE [Role_Ducks_Administracao];
GO

-- 4. ASSOCIAÇÃO DE USUÁRIOS ÀS ROLES
ALTER ROLE [Role_Ducks_Moderacao] ADD MEMBER [DbUser_Moderador];
GO

ALTER ROLE [Role_Ducks_Curadoria] ADD MEMBER [DbUser_Curador];
GO

ALTER ROLE [Role_Ducks_Administracao] ADD MEMBER [DbUser_Admin];
GO