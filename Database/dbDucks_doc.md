# Documentação do Banco de Dados - Projeto Ducks (`ducks-db`)

O banco de dados **`ducks-db`** foi projetado para sustentar uma plataforma educacional gamificada de aprendizado e avaliação de tecnologia. Ele gerencia trilhas de aprendizado, aulas, questões, provas, sistema de pontuação por XP, níveis de gamificação, conquistas de usuários e papéis de acesso.

---

## Visão Geral da Arquitetura e Relacionamentos

O esquema utiliza **SQL Server** e baseia-se nos seguintes pilares principais:

1. **Gestão de Usuários e Autenticação:**
   * **`Usuarios`**: Armazena as informações cadastrais dos usuários (com hash/salt para senhas).
   * **`Papeis`** e **`UsuarioPapeis`**: Implementam o controle de acesso baseado em papéis (RBAC), definindo permissões como Administrador, Curador e Moderador.

2. **Trilhas de Aprendizagem e Conteúdo:**
   * **`Materias`**: Categorias principais de estudo.
   * **`TrilhasAprendizagem`**: Trilhas de conhecimento associadas a matérias e moderadas por administradores/criadores.
   * **`Modulos`**: Subdivisões das trilhas.
   * **`Aulas`**: Conteúdos individuais pertencentes aos módulos.

3. **Avaliações e Questões:**
   * **`Questoes`**: Banco de questões (múltipla escolha ou verdadeiro/falso), associadas a matérias e módulos.
   * **`AlternativasQuestao`**: Opções de resposta para cada questão, contendo a indicação de qual é a correta.
   * **`Provas`**: Provas gerais ou específicas por matéria.
   * **`ProvaQuestoes`**: Tabela associativa que define a ordem das questões em cada prova.
   * **`ProvasUsuario`** e **`RespostasProvasUsuario`**: Registram as tentativas, status e respostas dadas pelos usuários nas avaliações.

4. **Gamificação e Progresso:**
   * **`XPUsuario`** e **`HistoricoXP`**: Controlam o saldo de pontos (XP) acumulados pelos usuários e o histórico detalhado de ganhos.
   * **`NiveisGamificacao`**: Definem os patamares e limites de XP para progressão de nível.
   * **`Conquistas`** e **`ConquistasUsuario`**: Gerenciam medalhas e o desbloqueio de premiações com base no desempenho.
   * **`ProgressoUsuario`** e **`UsuarioTrilhas`**: Acompanham a conclusão de aulas e o avanço percentual nas trilhas de aprendizado.

5. **Auditoria:**
   * **`LogsAuditoria`**: Registra eventos críticos do sistema, como acessos, alterações de conteúdo e moderação.

---

## Principais Tabelas e Dicionário de Dados

| Tabela | Propósito Principal |
| :--- | :--- |
| **`Usuarios`** | Armazena dados de identificação, e-mail e credenciais dos usuários da plataforma. |
| **`Materias`** | Agrupa os conteúdos por disciplinas ou áreas tecnológicas. |
| **`TrilhasAprendizagem`** | Define as jornadas de ensino estruturadas para os alunos. |
| **`Modulos`** | Agrupadores sequenciais dentro de uma trilha de aprendizagem. |
| **`Aulas`** | Unidades de ensino vinculadas aos módulos. |
| **`Questoes`** | Repositório de perguntas para simulados e provas. |
| **`AlternativasQuestao`** | Opções de resposta vinculadas às questões. |
| **`Provas`** | Configuração de avaliações gerais ou específicas. |
| **`ProvasUsuario`** | Sessões de provas realizadas pelos usuários. |
| **`XPUsuario`** | Saldo atual de experiência (XP) e nível de cada usuário. |
| **`Conquistas`** | Catálogo de medalhas/conquistas disponíveis na plataforma. |
| **`LogsAuditoria`** | Trilha de auditoria para ações administrativas e de segurança. |

---

## Stored Procedures de Destaque

O banco conta com procedimentos armazenados robustos com controle de transação (`XACT_ABORT`) para garantir a consistência dos dados:

* **`sp_AutenticarUsuario`**: Valida credenciais (com hash e salt seguros) e registra logs de sucesso ou falha de login.
* **`sp_CriarSessaoProvaGeral`**: Realiza o sorteio de questões ativas e aprovadas em todo o conjunto da plataforma para gerar simulados gerais.
* **`sp_CriarSessaoProvaEspecifica`**: Sorteia questões exclusivas de uma matéria específica para a criação de avaliações direcionadas.
* **`sp_SubmeterRespostaProva`**: Processa respostas, valida gabaritos, bloqueia submissões duplicadas, calcula o ganho de XP e atualiza automaticamente o nível do utilizador.
* **`sp_AprovarConteudo`**: Gerencia o fluxo de moderação de trilhas, aulas e questões, exigindo papéis ativos de Moderador ou Administrador.
* **`sp_InscreverUsuarioTrilha`**: Realiza a matrícula do aluno em uma trilha aprovada, garantindo proteção contra condições de corrida (*race conditions*) na duplicação.
* **`sp_VerificarEConcederConquistas`**: Verifica regras de desempenho (como acúmulo de XP ou conclusão de trilhas) e concede medalhas/conquistas automaticamente.
* **`sp_RelatorioGerencialResumo`**: Compila indicadores operacionais da plataforma (usuários ativos, trilhas, aulas, questões pendentes e total de XP distribuído).