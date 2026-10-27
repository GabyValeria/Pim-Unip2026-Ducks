# 📦 Funcionalidades e Módulos do Sistema — Ducks Admin Desktop

O **Ducks Admin Desktop** é a central de operações e gerenciamento da plataforma educacional gamificada **Ducks**. O aplicativo possibilita aos administradores governar todos os aspectos operacionais, pedagógicos e de segurança do sistema.

---
---

## 1. Detalhamento dos Módulos

### 1.1. Módulo de Autenticação e Segurança (LoginWindow)
* **Descrição:** Ponto de entrada no sistema desktop. Valida o e-mail e a senha do administrador via API, gerando e armazenando o token de sessão JWT.
* **Fluxo de Tela:**
  1. O usuário abre o aplicativo e visualiza a tela de login estilizada.
  2. Informa e-mail institucional (ex: `admin@ducks.com`) e senha.
  3. Clica em "Entrar no Sistema".
  4. O sistema valida as credenciais contra a API HTTP. Se válido, o token JWT é guardado em memória e a `MainWindow` é aberta.
* **Regras de Validação:**
  * O campo de e-mail deve ter um formato válido.
  * A senha não pode estar em branco.
  * Apenas usuários com a Claim `Role = Admin` ou `Role = Moderador` possuem acesso autorizado ao painel.

---

### 1.2. Módulo de Gestão de Usuários e Permissões (`UsersViewModel`)
* **Descrição:** Administração do ciclo de vida das contas cadastradas na plataforma (Alunos, Autores, Moderadores e Administradores).
* **Funcionalidades:**
  * **Listagem e Pesquisa:** Tabela paginada e filtro por nome, e-mail ou perfil de acesso.
  * **Alteração de Status:** Botão de ação direta para Ativar ou Bloquear/Desativar contas.
  * **Gestão de Roles:** Atribuição e alteração de níveis de permissão (Administrador, Moderador, Aluno, Visitante).
* **Regras de Negócio e Validação:**
  * Um administrador não pode desativar a própria conta enquanto estiver logado.
  * O e-mail do usuário deve ser único em toda a base de dados.

---

### 1.3. Módulo de Cursos e Trilhas Educacionais (`CursosTrilhasViewModel`)
* **Descrição:** Gestão do catálogo pedagógico da plataforma Ducks, permitindo criar e sequenciar a jornada de aprendizado dos alunos.
* **Funcionalidades:**
  * **CRUD de Cursos/Trilhas:** Inclusão, edição, visualização e arquivamento de cursos.
  * **Atribuição de Categoria e Carga Horária:** Organização por tópicos (ex: *Programação, Engenharia de Software, Design Educacional*).
  * **Publicação Contratada:** Alternar o status da trilha entre `Rascunho` e `Publicado`.
* **Regras de Negócio:**
  * Trilhas em estado `Rascunho` não são visíveis para os alunos na Plataforma Web/Mobile.
  * Uma trilha deve conter ao menos um módulo e uma aula cadastrados antes de ser publicada.

---

### 1.4. Módulo de Atividades Gamificadas e Pontuação (XP & Conquistas)
* **Descrição:** Configuração dos parâmetros de recompensa gamificada que incentivam o engajamento dos estudantes.
* **Funcionalidades:**
  * **Auditoria de XP:** Consulta e ajuste manual de pontos de XP para resolução de suporte ou bonificações em eventos.
  * **Gerenciamento de Badges/Medalhas:** Configuração dos requisitos para conquista de selos e conquistas.
* **Regras de Validação:**
  * O valor de XP concedido manualmente deve ser um número inteiro positivo.
  * O motivo da concessão de XP manual deve ser obrigatoriamente registrado no log de auditoria.

---

### 1.5. Módulo de Fila de Moderação (`ModerationViewModel`)
* **Descrição:** Central de moderação comunitária para triagem de denúncias e validação de conteúdos submetidos por autores/alunos.
* **Funcionalidades:**
  * **Visualização da Fila:** Lista de postagens, comentários ou materiais sob denúncia com status `PENDENTE`.
  * **Ações Rápidas:** Botões para `Aprovar` (manter publicado) ou `Rejeitar` (remover e notificar o autor).
* **Comportamento Esperado:**
  * Ao aprovar/rejeitar, o item sai imediatamente da fila pendente através de atualização da `ObservableCollection` no ViewModel.

---

### 1.6. Módulo de Métricas e Dashboards (`DashboardViewModel`)
* **Descrição:** Painel analítico executivo que exibe os Indicadores Chave de Desempenho (KPIs) da plataforma em tempo real.
* **Métricas Exibidas:**
  * Total de Usuários Cadastrados e Crescimento Percentual.
  * Volume de Atividades Realizadas no Mês.
  * Indicador de Conexão SignalR/WebSocket (Status da API).
  * Gráfico de Distribuição e Retenção de Alunos.

---

### 1.7. Módulo Assistente de Inteligência Artificial (`AiAssistantViewModel`)
* **Descrição:** Ferramenta assistiva integrada para geração automatizada de relatórios, resumos pedagógicos e apoio à moderação.
* **Funcionalidades:**
  * **Gerador de Resumos:** Criação de resumos executivos baseados no desempenho das turmas.
  * **Sugestões Pedagógicas:** Geração automática de ideias para novos desafios gamificados.

---

### 1.8. Módulo de Configurações e Parâmetros (`ConfiguracoesViewModel`)
* **Descrição:** Painel técnico para ajustes globais de infraestrutura da aplicação desktop.
* **Funcionalidades:**
  * Alteração da URL Base da API REST (`Azure App Service`).
  * Ativação/Desativação do Modo de Manutenção da plataforma.
  * Alternância dos alertas push via SignalR.