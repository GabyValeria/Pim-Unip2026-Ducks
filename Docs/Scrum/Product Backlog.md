# Product Backlog — Projeto Ducks 

## Visão Geral
Este documento apresenta o Product Backlog Final do ecossistema educacional gamificado **Ducks*. Todas as Histórias de Usuário foram implementadas, testadas, integradas à API ASP.NET Core e validadas na aplicação cliente WPF (Painel Administrativo) e nos demais ecossistemas.

---

## Definition of Done (DoD) do Projeto
Todas as histórias catalogadas como `CONCLUÍDA` atenderam rigorosamente aos critérios abaixo:
- [x] Código implementado seguindo Clean Architecture em .NET / C#.
- [x] Cobertura de testes unitários superior a 80%.
- [x] Integração completa via Endpoints REST JWT com a API central em nuvem.
- [x] Interface gráfica em WPF com padrão MVVM, responsiva e sem congelamento de UI (processamento assíncrono via `async/await`).
- [x] Build limpo de pipeline (CI/CD) e deploy realizado no Azure App Service / Azure SQL.

---

## Épico 01: Identidade, Segurança e Acesso (IAM)

### 🔹 US01 - Autenticação e Token JWT no WPF
* **Como** Administrador / Professor / Estudante
* **Quero** realizar login seguro utilizando email e senha na aplicação
* **Para** acessar os recursos do sistema Ducks de acordo com meu nível de permissão (RBAC).
* **Critérios de Aceitação:**
  1. A API deve validar as credenciais via hash/salt com `PasswordHasher`.
  2. A API deve retornar um JWT Bearer Token com tempo de expiração e claims do usuário.
  3. O WPF deve armazenar o token em memória com segurança durante a sessão.
  4. Credenciais inválidas devem exibir alerta claro na tela sem revelar detalhes internos.
* **Status:** `CONCLUÍDA` (DoD Cumprido)

### 🔹 US02 - Gestão de Usuários e Permissões no WPF
* **Como** Administrador do Sistema
* **Quero** listar, filtrar, editar perfis e bloquear usuários através do app WPF
* **Para** manter o controle de acesso e segurança da comunidade acadêmica.
* **Critérios de Aceitação:**
  1. Tabela paginada listando Estudantes, Professores e Moderadores no WPF.
  2. Modal seguro de confirmação antes de desativar ou redefinir senha de usuários.
  3. Validação de formato de email e preenchimento obrigatório de campos.
* **Status:** `CONCLUÍDA` (DoD Cumprido)

---

## Épico 02: Gestão Educacional e Conteúdo Interativo

### 🔹 US03 - Criação e Gestão de Trilhas de Aprendizagem
* **Como** Professor / Administrador
* **Quero** cadastrar, editar e organizar Trilhas de Aprendizagem e suas Aulas
* **Para** disponibilizar conteúdos sequenciais aos estudantes.
* **Critérios de Aceitação:**
  1. Cadastro de título, descrição, categoria e requisitos de acesso.
  2. Vinculação de Aulas com suporte a conteúdo textual e links de materiais.
  3. Exclusão lógica ou alteração de status da trilha (Rascunho vs. Publicado).
* **Status:** `CONCLUÍDA` (DoD Cumprido)

---

## Épico 03: Motor de Gamificação e Engine de XP

### 🔹 US04 - Atribuição e Histórico de XP por Atividades
* **Como** Estudante
* **Quero** receber pontuação em XP automaticamente ao concluir aulas e avaliações
* **Para** evoluir de nível dentro da plataforma sem dependência de rankings competitivos.
* **Critérios de Aceitação:**
  1. A API deve processar o acúmulo de XP garantindo idempotência (não duplicar XP da mesma aula).
  2. Registro histórico detalhado (`HistoricoXP`) no Azure SQL.
  3. Visualização do progresso atual de XP no app WPF e interfaces do ecossistema.
* **Status:** `CONCLUÍDA` (DoD Cumprido)

---

## Épico 04: Moderação em Tempo Real e Dashboard de Métricas

### 🔹 US05 - Dashboard Administrativo com Telemetria e SignalR
* **Como** Administrador
* **Quero** visualizar indicadores de desempenho, usuários ativos e alertas de moderação em tempo real
* **Para** monitorar a saúde da plataforma e agir rapidamente contra conteúdos impróprios.
* **Critérios de Aceitação:**
  1. Conexão via WebSockets/SignalR entre API e aplicativo WPF.
  2. Atualização automática dos cards de métricas em `MetricsDashboardView.xaml`.
  3. Interface gráfica responsiva com gráficos e tabelas atualizadas sem necessidade de F5/refresh.
* **Status:** `CONCLUÍDA` 