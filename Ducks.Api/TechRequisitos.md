# Especificações Técnicas e Requisitos do Ducks API

Este documento detalha o ecossistema tecnológico, requisitos funcionais/não-funcionais e pré-requisitos para execução da API Ducks.

---

## 1. Stack Tecnológico

### Backend & API
* **Runtime:** .NET 10.0 (LTS/Latest Core Runtime).
* **Linguagem:** C# 13.
* **Arquitetura:** Clean Architecture (Domain-Driven Design Pattern).
* **Acesso a Dados:** ADO.NET otimizado com execução direta de **Stored Procedures** com controle transacional (`XACT_ABORT`).
* **Autenticação & Autorização:** JSON Web Token (JWT) + RBAC (Role-Based Access Control).
* **Criptografia de Senhas:** `Microsoft.AspNetCore.Identity.PasswordHasher<T>` (PBKDF2 com HMAC-SHA256/SHA512 e Salt dinâmico).
* **Documentação:** Swashbuckle / Swagger OpenAPI v3.

### Banco de Dados
* **SGBD:** Azure SQL Database (Microsoft SQL Server Engine).
* **Recursos Avançados:** Stored Procedures, Views, Triggers, Índices Exclusivos e Tabelas de Auditoria Imutáveis.

### Infraestrutura e Nuvem
* **Provedor:** Microsoft Azure.
* **Hospedagem API:** Azure App Service (Plano de serviço Windows / Kestrel).
* **Banco de Dados:** Azure SQL Server (Resource Group unificado `rg-ducks-dev`).
* **Logs & Monitoramento:** Azure Log Stream & Diagnostics.

---

## 2. Requisitos do Sistema

### Requisitos Funcionais (RF)

#### IAM (Identity & Access Management)
* **RF01:** Permitir cadastro de novos usuários com validação de unicidade de e-mail.
* **RF02:** Autenticar usuários via e-mail e senha, retornando token JWT assinado.
* **RF03:** Garantir o controle de acesso por papéis (*Administrador*, *Curador*, *Moderador*, *Estudante*).

#### Conteúdo Educacional e Trilhas
* **RF04:** Permitir a criação e organização de Matérias, Trilhas de Aprendizagem, Módulos e Aulas.
* **RF05:** Controlar o fluxo de moderação de trilhas e aulas (*Pendente*, *Aprovado*, *Rejeitado*).
* **RF06:** Registrar a inscrição do usuário em trilhas e calcular seu progresso percentual.

#### Avaliações e Provas
* **RF07:** Manter banco de questões categorizadas por matéria, peso e nível de dificuldade (*Fácil*, *Médio*, *Difícil*).
* **RF08:** Permitir a geração de simulados gerais ou específicos por matéria através de sorteio automatizado.
* **RF09:** Processar submissões de respostas de provas, impedindo submissões duplicadas na mesma questão.

#### Gamificação e Progresso
* **RF10:** Computar automaticamente o acúmulo de XP ao acertar questões ou concluir conteúdos.
* **RF11:** Calcular a evolução de nível do usuário com base nos limiares de XP.
* **RF12:** Conceder medalhas/conquistas automaticamente quando o usuário atingir marcos do sistema.

#### Auditoria
* **RF13:** Registrar logs imutáveis de ações críticas (logins, alterações de permissões e moderação).

---

### Requisitos Não-Funcionais (RNF)

* **RNF01 (Desempenho):** A API deve responder a 95% das requisições de leitura em menos de 200ms.
* **RNF02 (Segurança):** Nenhuma senha deve ser armazenada em texto plano.
* **RNF03 (Segurança):** Endpoints protegidos devem recusar requisições sem o cabeçalho `Authorization: Bearer <token>`.
* **RNF04 (Concorrência):** Operações de submissão e matrícula devem ser protegidas contra *race conditions*.
* **RNF05 (Escalabilidade):** A API deve ser *stateless*, permitindo dimensionamento horizontal (*Scale-Out*) no Azure App Service.

---

## 3. Pré-requisitos para Desenvolvimento Local

Para rodar o projeto localmente, certifique-se de ter instalado:

1. **.NET SDK 10.0** ou superior.
2. **Visual Studio 2022** (v17.10+) ou **VS Code** com extensão C#.
3. **SQL Server 2022** local ou acesso ao **Azure SQL Database**.
4. **Azure CLI** (opcional, para deploys via terminal).
5. **Postman** ou **Insomnia** (opcional, para testes de integração).