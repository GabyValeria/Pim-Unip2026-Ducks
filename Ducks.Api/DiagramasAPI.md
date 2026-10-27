# Diagramas de Arquitetura e Fluxos do Ducks API

Este documento contém a representação visual da arquitetura e dos fluxos operacionais da **Ducks API**.

---

## 1. Arquitetura da Solução (Clean Architecture)

A API foi desenvolvida seguindo os princípios de **Clean Architecture** em **.NET 10**, garantindo o desacoplamento das regras de negócio em relação à infraestrutura e apresentação.

```mermaid
graph TD
    UI[Clientes Web / Desktop / Mobile] -->|HTTP / REST + JWT| API[Ducks.Api - Presentation Layer]
    API --> Application[Ducks.Application - Use Cases & DTOs]
    Application --> Domain[Ducks.Domain - Entities, Interfaces & Domain Logic]
    
    API --> Infra[Ducks.Infrastructure - Data Access & Services]
    Infra -->|Dependency Injection| Application
    Infra -->|ADO.NET / Stored Procedures| DB[(Azure SQL Database)]
    
    subgraph Core Domain
        Domain
    end

    subgraph Application Logic
        Application
    end

    subgraph Infrastructure & Persistence
        Infra
        DB
    end
```

---

## 2. Fluxo de Autenticação JWT

```mermaid
sequenceDiagram
    autonumber
    actor User as Cliente / Usuário
    participant API as Ducks.Api (/api/auth/login)
    participant DB as Stored Procedure (sp_AutenticarUsuario)
    participant JWT as JwtTokenService

    User->>API: POST /api/auth/login { Email, Senha }
    API->>DB: Executa sp_AutenticarUsuario
    DB-->>API: Retorna Usuário, Hash, Salt e Status
    API->>API: Valida Hash via PasswordHasher<T>
    alt Credenciais Válidas
        API->>JWT: GerarToken(Usuario)
        JWT-->>API: Retorna Token Bearer (Claims + Validade)
        API-->>User: 200 OK { token, expiration, user }
    else Credenciais Inválidas
        API-->>User: 401 Unauthorized { message }
    end
```

---

## 3. Fluxo de Execução de Prova e Recompensa de Gamificação

```mermaid
sequenceDiagram
    autonumber
    actor User as Estudante
    participant API as Ducks.Api (/api/educacional/provas/submeter)
    participant SP as Stored Procedure (sp_SubmeterRespostaProva)
    participant DB as Tabela XPUsuario & HistoricoXP

    User->>API: POST /api/educacional/provas/submeter { ProvaUsuarioID, QuestaoID, AlternativaID }
    API->>SP: Executa sp_SubmeterRespostaProva
    Note over SP: Valida Gabarito & Impede Submissão Duplicada
    alt Resposta Correta
        SP->>DB: Incrementa XP em XPUsuario
        SP->>DB: Insere registro em HistoricoXP
        SP->>DB: Avalia e atualiza NivelGamificacao
        SP->>DB: Executa sp_VerificarEConcederConquistas
        SP-->>API: Retorna { EhCorreta: true, XPGanho: N, NovoNivel: X }
    else Resposta Incorreta
        SP-->>API: Retorna { EhCorreta: false, XPGanho: 0 }
    end
    API-->>User: 200 OK (Resultado da Questão + Status de Gamificação)
```