# Ducks.Admin.Desktop — Painel de Gestão Administrativa

O **Ducks.Admin.Desktop** é uma aplicação de desktop de alta performance desenvolvida em **C#**, **.NET** e **WPF** (Windows Presentation Foundation), utilizando o padrão arquitetural **MVVM** (`CommunityToolkit.Mvvm`). Trata-se da plataforma exclusiva para administradores, curadores e gestores realizarem a moderação e o controle interno de todo o ecossistema educacional **Ducks**.

---

## Regra de Ouro da Arquitetura
O aplicativo desktop **NUNCA** acessa diretamente o banco de dados SQL Server (`ducks-db`). Toda a comunicação é realizada obrigatoriamente de forma segura por meio de requisições HTTPS/REST (utilizando `Refit`) e WebSockets em tempo real (`SignalR`) com a API Principal (`Ducks.Api`).

---

## O que o Sistema Faz (Funcionalidades Principais)

1. **Autenticação Corporativa Segura:**
   - Tela de login exclusiva para administradores com validação de credenciais e emissão/armazenamento de tokens JWT.

2. **Gestão e Moderação de Usuários:**
   - Listagem de alta performance utilizando tabelas otimizadas (`DataGrid`).
   - Paginação e filtros dinâmicos por termo de busca.
   - Sistema de moderação com capacidade de bloquear ou desbloquear contas de usuários e autores instantaneamente.

3. **Gerenciamento de Conteúdo Educacional:**
   - Administração, cadastro e fluxo de aprovação de Trilhas de Aprendizagem e módulos educacionais do ecossistema.

4. **Configuração de Gamificação:**
   - Gerenciamento de conquistas, medalhas e patamares de Experiência (XP) para engajamento dos alunos (sem qualquer tipo de ranking ou competição).

5. **Assistente de IA Integrado:**
   - Painel inteligente conectado via API para auxiliar o administrador na revisão de conteúdos, moderação assistida e automação de tarefas repetitivas.

6. **Métricas em Tempo Real e Auditoria:**
   - Monitoramento instantâneo de métricas (como usuários ativos online) via conexão bidirecional SignalR.
   - Registro e visualização de logs de auditoria detalhando as ações executadas por administradores no sistema.