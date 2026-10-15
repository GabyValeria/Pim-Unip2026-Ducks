# Modelo Entidade-Relacionamento (MER) - Projeto Ducks (`ducks-db`)

O ecossistema de dados da plataforma **Ducks** está estruturado em cinco domínios funcionais principais, garantindo a modularidade, integridade referencial e suporte aos fluxos de gamificação, educação e segurança.

---

## 1. Domínio de Identidade e Controle de Acessos (IAM)

Este domínio é responsável pela gestão de utilizadores, credenciais e controlo de acessos baseado em papéis (*RBAC*).

* **`Usuarios`**
  * **Propósito:** Armazena o cadastro central de todos os utilizadores da plataforma (estudantes, curadores, moderadores e administradores).
  * **Atributos Principais:** `UsuarioID` (PK), `NomeUsuario`, `Email`, `HashSenha`, `SaltSenha`, `EstaAtivo`, `CriadoEm`, `AtualizadoEm`.
  * **Segurança:** As senhas são armazenadas exclusivamente através de hash seguro combinado com *salt* (`HashSenha` e `SaltSenha`) gerenciados por procedimentos de autenticação nativos.

* **`Papeis`**
  * **Propósito:** Define as funções lógicas disponíveis no sistema (ex: Administrador, Curador, Moderador).
  * **Atributos Principais:** `PapelID` (PK), `NomePapel`, `Descricao`, `CriadoEm`.

* **`UsuarioPapeis`**
  * **Propósito:** Tabela associativa (N:N) que vincula os utilizadores aos seus respetivos papéis de permissão.
  * **Atributos Principais:** `UsuarioID` (PK, FK), `PapelID` (PK, FK), `AtribuidoEm`.

---

## 2. Domínio de Conteúdo Educacional e Trilhas

Gerencia a árvore hierárquica de ensino, desde as grandes áreas do conhecimento até aos conteúdos práticos das aulas.

* **`Materias`**
  * **Propósito:** Agrupa as disciplinas macro abordadas na plataforma tecnológica.
  * **Atributos Principais:** `MateriaID` (PK), `NomeMateria`, `Descricao`.

* **`TrilhasAprendizagem`**
  * **Propósito:** Jornadas de aprendizagem estruturadas vinculadas a uma matéria específica.
  * **Atributos Principais:** `TrilhaID` (PK), `NomeTrilha`, `Descricao`, `MateriaID` (FK), `Status` (*PENDENTE*, *APROVADO*, *REJEITADO*), `CriadoPorUsuarioID` (FK), `AprovadoPorUsuarioID` (FK), `AtualizadoEm`.

* **`Modulos`**
  * **Propósito:** Subdivisões lógicas e sequenciais dentro de uma trilha de aprendizagem.
  * **Atributos Principais:** `ModuloID` (PK), `TrilhaID` (FK), `Titulo`, `IndiceOrdem`.

* **`Aulas`**
  * **Propósito:** Unidades individuais de ensino pertencentes a um módulo.
  * **Atributos Principais:** `AulaID` (PK), `ModuloID` (FK), `Titulo`, `UrlConteudo`, `IndiceOrdem`, `Status`, `CriadoPorUsuarioID` (FK), `AprovadoPorUsuarioID` (FK).

* **`UsuarioTrilhas` & `ProgressoUsuario`**
  * **Propósito:** Monitorizam o progresso de inscrição, percentual concluído e a finalização de aulas individuais pelos estudantes.
  * **Atributos Principais:** Relacionam utilizadores a trilhas e aulas, guardando carimbos de data/hora de conclusão e percentuais de avanço.

---

## 3. Domínio de Avaliações, Provas e Questões

Responsável pela criação de perguntas, alternativas, montagem de simulados e correção automática de provas.

* **`Questoes`**
  * **Propósito:** Banco central de perguntas técnicas (múltipla escolha ou verdadeiro/falso).
  * **Atributos Principais:** `QuestaoID` (PK), `MateriaID` (FK), `TextoQuestao`, `TipoQuestao`, `Peso`, `NivelDificuldade` (*FACIL*, *MEDIO*, *DIFICIL*), `Status`.

* **`AlternativasQuestao`**
  * **Propósito:** Opções de resposta vinculadas a cada questão.
  * **Atributos Principais:** `AlternativaID` (PK, FK), `QuestaoID` (PK, FK), `TextoAlternativa`, `EhCorreta` (Booleano). Possui índices exclusivos que garantem a existência de alternativas válidas e de resposta única correta.

* **`Provas` & `ProvaQuestoes`**
  * **Propósito:** Configuradores de exames (gerais ou específicos por matéria) e a tabela de associação que determina a ordem das questões sorteadas para o teste.

* **`ProvasUsuario` & `RespostasProvasUsuario`**
  * **Propósito:** Registam as sessões de exames iniciadas pelos utilizadores e cada alternativa submetida, validando o acerto e bloqueando tentativas duplicadas na mesma questão.

---

## 4. Domínio de Gamificação e Recompensas

Incentiva a retenção e o engajamento dos utilizadores através de pontos de experiência (XP), níveis e medalhas.

* **`XPUsuario` & `HistoricoXP`**
  * **Propósito:** Mantêm o saldo acumulado de pontos de experiência por utilizador e o registo histórico auditável de cada ganho de XP (por exemplo, ao responder corretamente a questões).

* **`NiveisGamificacao`**
  * **Propósito:** Tabela de referência que define os patamares de progressão do utilizador com base em limites mínimos de XP (`XPMinimo`).

* **`Conquistas` & `ConquistasUsuario`**
  * **Propósito:** Catálogo de medalhas e prémios desbloqueados automaticamente por procedimentos armazenados quando os utilizadores atingem determinados marcos de desempenho (*Primeiro Passo*, *Explorador Focado*, *Desbravador*).

---

## 5. Domínio de Auditoria e Operações

* **`LogsAuditoria`**
  * **Propósito:** Registo imutável de eventos críticos do sistema, incluindo acessos bem-sucedidos ou falhados, moderação de conteúdos e ações administrativas.
  * **Atributos Principais:** `LogID` (PK), `UsuarioID` (FK), `Acao`, `Detalhes`, `EnderecoIP`, `CriadoEm`.