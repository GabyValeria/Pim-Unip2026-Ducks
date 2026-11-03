# Histórico de Sprints (Reviews & Retrospectives) — Projeto Ducks

Este documento detalha o histórico de execução ágil do ecossistema Ducks, cobrindo o ciclo de vida desde a fundação da arquitetura até o lançamento da v1.0.0.

---

## Resumo do Ciclo de Sprints

| Sprint | Objetivos Principais | Story Points Entregues | Velocidade | Status |
| :---: | :--- | :---: | :---: | :---: |
| **Sprint 1** | Arquitetura Backend ASP.NET Core, EF Core e Banco de Dados Azure SQL | 34 pts | 34 pts | Concluído |
| **Sprint 2** | Autenticação JWT, Regras de XP, Tests Unitários e Deploy Azure | 40 pts | 40 pts | Concluído |
| **Sprint 3** | Cliente Desktop WPF (MVVM), UI/UX, Moderação SignalR e Encerramento | 46 pts | 46 pts | Concluído |

---

## Sprint 1: Fundação do Backend e Persistência
* **Duração:** 2 Semanas
* **Foco:** Definição da Clean Architecture, modelagem do banco de dados `ducks-db` e contratos iniciais da API.

### Sprint Review
* **Entregas de Valor:**
  * Estrutura em 4 camadas (`Domain`, `Application`, `Infrastructure`, `Api`) criada com sucesso.
  * Scripts e Migrations do Entity Framework Core executados no Azure SQL Database.
  * Endpoints base de CRUD para Usuários, Trilhas e Aulas operacionais.

### Retrospective
* **O que funcionou bem:**
  * Excelente alinhamento entre o PO e a equipe de desenvolvimento na modelagem do domínio.
* **Pontos de Melhoria:**
  * Incompatibilidade inicial de versões de pacotes NuGet em C# .NET.
* **Ações de Ajuste Superadas:**
  * Padronização de todas as bibliotecas do ecossistema e fixação do target framework.

---

## Sprint 2: Segurança, Gamificação e Implantação em Nuvem
* **Duração:** 2 Semanas
* **Foco:** Autenticação por Tokens JWT, cálculo de XP e pipeline CI/CD.

### Sprint Review
* **Entregas de Valor:**
  * Módulo de autenticação com hash/salt seguro de senhas via `PasswordHasher`.
  * Motor de XP implementado sem cálculo de rankings, atendendo exatamente o RF04.
  * Deploy bem-sucedido do backend no Azure App Service com HTTPS e Swagger ativo.

### Retrospective
* **O que funcionou bem:**
  * Testes unitários com `xUnit` e `EF Core InMemory` garantiram regressão zero durante refatorações.
* **Pontos de Melhoria:**
  * Erros de inicialização da API no Azure devido à falta de configuração adequada da connection string em variáveis de ambiente.
* **Ações de Ajuste Superadas:**
  * Configuração de App Settings diretamente no Azure Portal com criptografia at-rest.

---

## Sprint 3: Cliente WPF Admin, SignalR e Polimento Final
* **Duração:** 2 Semanas
* **Foco:** Desenvolvimento do aplicativo desktop WPF com padrão MVVM e moderação em tempo real.

### Sprint Review
* **Entregas de Valor:**
  * Desenvolvimento completo da interface WPF (`Ducks.Desktop.UI`) integrada ao `Ducks.Desktop.Application`.
  * Implementação do `IDialogService` para gerenciar caixas de mensagem desacopladas sem violar o padrão MVVM.
  * Dashboard de métricas e hub de moderação em tempo real com `SignalR`.

### Retrospective
* **O que funcionou bem:**
  * Resolução célere de erros de compilação (ex: desacoplamento de `MessageBox` das ViewModels).
* **Pontos de Melhoria:**
  * Vulnerabilidade em versões antigas do pacote `SignalR.Common`.
* **Ações de Ajuste Superadas:**
  * Atualização dos pacotes e garantia de zero erros/avisos na compilação do `dotnet build`.