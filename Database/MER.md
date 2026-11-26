# Modelo Entidade-Relacionamento (MER) — Projeto Ducks (`ducks-db`)

## 1. Entidades por Domínio

* **Segurança, Identidade e Governança:**
  * `Usuarios`
  * `Papeis`
  * `UsuarioPapeis`
  * `RefreshTokens`
  * `TokensRecuperacaoSenha`
  * `LogsAuditoria`
  * `ConfiguracoesPlataforma`
  * `ConfiguracoesSistema`

* **Conteúdo Pedagógico e Progresso:**
  * `Materias`
  * `TrilhasAprendizagem`
  * `Modulos`
  * `Aulas`
  * `RevisoesConteudo`
  * `UsuarioTrilhas`
  * `ProgressoUsuario`

* **Avaliação e Simulados:**
  * `Questoes`
  * `AlternativasQuestao`
  * `Provas`
  * `ProvaQuestoes`
  * `ProvasUsuario`
  * `RespostasProvasUsuario`

* **Gamificação e Recompensas:**
  * `NiveisGamificacao`
  * `XPUsuario`
  * `HistoricoXP`
  * `Conquistas`
  * `ConquistasUsuario`
  * `UsuarioConquistas`
  * `Desafios`
  * `ParticipacoesDesafio`
  * `RecompensasConcedidas`

* **Assistente Pedagógico (Chat):**
  * `ChatConversations`
  * `ChatMessages`

---

## 2. Atributos, Identificadores e Restrições de Unicidade por Entidade

### 2.1 Segurança, Identidade e Governança

* **`Usuarios`**
  * **PK:** `UsuarioID` (`int`, identity)
  * **UK:** `NomeUsuario` (`nvarchar(100)`), `Email` (`nvarchar(255)`)
  * **Atributos:** `EstaAtivo` (`bit`), `CriadoEm` (`datetime2(7)`), `AtualizadoEm` (`datetime2(7)`), `SenhaHash` (`nvarchar(255)`), `SenhaAlteradaEm` (`datetime2(7)`)

* **`Papeis`**
  * **PK:** `PapelID` (`int`, identity)
  * **UK:** `NomePapel` (`nvarchar(50)`)
  * **Atributos:** `Descricao` (`nvarchar(255)`), `CriadoEm` (`datetime2(7)`)

* **`UsuarioPapeis`**
  * **PK Composta:** (`UsuarioID` `int`, `PapelID` `int`)
  * **Atributos:** `AtribuidoEm` (`datetime2(7)`)

* **`RefreshTokens`**
  * **PK:** `Id` (`uniqueidentifier`)
  * **UK:** `TokenHash` (`char(64)`)
  * **Atributos:** `UsuarioID` (`int`), `TokenFamilyId` (`uniqueidentifier`), `CriadoEm` (`datetime2(7)`), `ExpiraEm` (`datetime2(7)`), `RevogadoEm` (`datetime2(7)`), `SubstituidoPorId` (`uniqueidentifier`), `Dispositivo` (`nvarchar(150)`), `RowVersion` (`timestamp`)

* **`TokensRecuperacaoSenha`**
  * **PK:** `TokenID` (`int`, identity)
  * **UK:** `TokenHash` (`char(64)`)
  * **Atributos:** `UsuarioID` (`int`), `CriadoEm` (`datetime2(7)`), `ExpiraEm` (`datetime2(7)`), `UsadoEm` (`datetime2(7)`)

* **`LogsAuditoria`**
  * **PK:** `LogID` (`int`, identity)
  * **Atributos:** `UsuarioID` (`int`), `Acao` (`nvarchar(100)`), `Detalhes` (`nvarchar(max)`), `EnderecoIP` (`nvarchar(50)`), `CriadoEm` (`datetime2(7)`)

* **`ConfiguracoesPlataforma`**
  * **PK:** `ConfiguracaoID` (`int`, identity)
  * **UK:** `Chave` (`nvarchar(100)`)
  * **Atributos:** `Valor` (`nvarchar(max)`), `Descricao` (`nvarchar(255)`), `AtualizadoEm` (`datetime2(7)`), `AtualizadoPorUsuarioID` (`int`)

* **`ConfiguracoesSistema`**
  * **PK:** `Chave` (`nvarchar(100)`)
  * **Atributos:** `Valor` (`nvarchar(4000)`), `AtualizadoEm` (`datetime2(7)`), `AtualizadoPor` (`int`)

---

### 2.2 Conteúdo Pedagógico e Progresso

* **`Materias`**
  * **PK:** `MateriaID` (`int`, identity)
  * **UK:** `NomeMateria` (`nvarchar(100)`)
  * **Atributos:** `Descricao` (`nvarchar(500)`)

* **`TrilhasAprendizagem`**
  * **PK:** `TrilhaID` (`int`, identity)
  * **Atributos:** `NomeTrilha` (`nvarchar(150)`), `Descricao` (`nvarchar(max)`), `MateriaID` (`int`), `Status` (`nvarchar(30)`), `CriadoPorUsuarioID` (`int`), `AprovadoPorUsuarioID` (`int`), `AtualizadoEm` (`datetime2(7)`)

* **`Modulos`**
  * **PK:** `ModuloID` (`int`, identity)
  * **Atributos:** `TrilhaID` (`int`), `Titulo` (`nvarchar(150)`), `IndiceOrdem` (`int`), `Descricao` (`nvarchar(2000)`), `Status` (`nvarchar(30)`), `CriadoEm` (`datetime2(7)`), `AtualizadoEm` (`datetime2(7)`)

* **`Aulas`**
  * **PK:** `AulaID` (`int`, identity)
  * **Atributos:** `ModuloID` (`int`), `Titulo` (`nvarchar(150)`), `UrlConteudo` (`nvarchar(500)`), `IndiceOrdem` (`int`), `Status` (`nvarchar(30)`), `CriadoPorUsuarioID` (`int`), `AprovadoPorUsuarioID` (`int`), `Conteudo` (`nvarchar(max)`), `AtualizadoEm` (`datetime2(7)`)

* **`RevisoesConteudo`**
  * **PK:** `RevisaoID` (`int`, identity)
  * **Atributos:** `TipoConteudo` (`nvarchar(20)`), `ConteudoID` (`int`), `Acao` (`nvarchar(30)`), `StatusAnterior` (`nvarchar(30)`), `StatusNovo` (`nvarchar(30)`), `Justificativa` (`nvarchar(1000)`), `UsuarioID` (`int`), `CriadoEm` (`datetime2(7)`)

* **`UsuarioTrilhas`**
  * **PK:** `UsuarioTrilhaID` (`int`, identity)
  * **UK:** (`UsuarioID` `int`, `TrilhaID` `int`)
  * **Atributos:** `InscritoEm` (`datetime2(7)`), `ConcluidoEm` (`datetime2(7)`), `ProgressoPercentual` (`decimal(5,2)`), `Status` (`nvarchar(30)`), `CanceladoEm` (`datetime2(7)`)

* **`ProgressoUsuario`**
  * **PK:** `ProgressoID` (`int`, identity)
  * **UK:** (`UsuarioID` `int`, `AulaID` `int`)
  * **Atributos:** `ConcluidoEm` (`datetime2(7)`)

---

### 2.3 Avaliação e Simulados

* **`Questoes`**
  * **PK:** `QuestaoID` (`int`, identity)
  * **Atributos:** `MateriaID` (`int`), `TextoQuestao` (`nvarchar(max)`), `TipoQuestao` (`nvarchar(50)`), `Peso` (`int`), `NivelDificuldade` (`nvarchar(20)`), `Status` (`nvarchar(30)`), `ModuloID` (`int`), `CriadoPorUsuarioID` (`int`), `AprovadoPorUsuarioID` (`int`), `TrilhaID` (`int`), `AulaID` (`int`), `Explicacao` (`nvarchar(2000)`), `Assunto` (`nvarchar(150)`), `Tags` (`nvarchar(500)`), `CriadoEm` (`datetime2(7)`), `AtualizadoEm` (`datetime2(7)`)

* **`AlternativasQuestao`**
  * **PK:** `AlternativaID` (`int`, identity)
  * **UK:** (`AlternativaID` `int`, `QuestaoID` `int`) e índice único filtrado por `QuestaoID` onde `EhCorreta = 1`
  * **Atributos:** `QuestaoID` (`int`), `TextoAlternativa` (`nvarchar(max)`), `EhCorreta` (`bit`), `Explicacao` (`nvarchar(1000)`)

* **`Provas`**
  * **PK:** `ProvaID` (`int`, identity)
  * **Atributos:** `Titulo` (`nvarchar(150)`), `TipoProva` (`nvarchar(30)`), `MateriaID` (`int`), `TotalQuestoes` (`int`), `CriadoEm` (`datetime2(7)`), `Descricao` (`nvarchar(2000)`), `TrilhaID` (`int`), `ModuloID` (`int`), `TempoLimiteMinutos` (`int`), `MaxTentativas` (`int`), `NotaMinimaAprovacao` (`int`), `RecompensaXP` (`int`), `Status` (`nvarchar(30)`), `CriadoPorUsuarioID` (`int`), `AtualizadoEm` (`datetime2(7)`)

* **`ProvaQuestoes`**
  * **PK Composta:** (`ProvaID` `int`, `QuestaoID` `int`)
  * **UK:** (`ProvaID` `int`, `OrdemQuestao` `int`)
  * **Atributos:** `OrdemQuestao` (`int`)

* **`ProvasUsuario`**
  * **PK:** `ProvaUsuarioID` (`int`, identity)
  * **Atributos:** `UsuarioID` (`int`), `ProvaID` (`int`), `IniciadoEm` (`datetime2(7)`), `ConcluidoEm` (`datetime2(7)`), `Pontuacao` (`int`), `Status` (`nvarchar(30)`), `NumeroTentativa` (`int`), `PrazoEm` (`datetime2(7)`), `TotalQuestoes` (`int`), `NotaPercentual` (`decimal(5,2)`), `XPGanho` (`int`)

* **`RespostasProvasUsuario`**
  * **PK:** `RespostaUsuarioID` (`int`, identity)
  * **UK:** (`ProvaUsuarioID` `int`, `QuestaoID` `int`)
  * **Atributos:** `AlternativaSelecionadaID` (`int`), `EhCorreta` (`bit`), `RespondidoEm` (`datetime2(7)`)

---

### 2.4 Gamificação e Recompensas

* **`NiveisGamificacao`**
  * **PK:** `NivelID` (`int`, identity)
  * **UK:** `NumeroNivel` (`int`), `XPMinimo` (`int`)
  * **Atributos:** `TituloNivel` (`nvarchar(50)`)

* **`XPUsuario`**
  * **PK:** `UsuarioID` (`int`)
  * **Atributos:** `TotalXP` (`int`), `NivelAtualID` (`int`), `AtualizadoEm` (`datetime2(7)`), `RowVersion` (`timestamp`)

* **`HistoricoXP`**
  * **PK:** `HistoricoXPID` (`int`, identity)
  * **Atributos:** `UsuarioID` (`int`), `QuantidadeXP` (`int`), `Motivo` (`nvarchar(100)`), `ReferenciaID` (`int`), `CriadoEm` (`datetime2(7)`)

* **`Conquistas`**
  * **PK:** `ConquistaID` (`int`, identity)
  * **UK:** `NomeConquista` (`nvarchar(100)`)
  * **Atributos:** `Descricao` (`nvarchar(255)`), `UrlIcone` (`nvarchar(500)`)

* **`ConquistasUsuario`**
  * **PK Composta:** (`UsuarioID` `int`, `ConquistaID` `int`)
  * **Atributos:** `ConquistadoEm` (`datetime2(7)`)

* **`UsuarioConquistas`**
  * **PK Composta:** (`UsuarioID` `int`, `ConquistaID` `int`)
  * **Atributos:** `ConquistadaEm` (`datetime2(7)`)

* **`Desafios`**
  * **PK:** `DesafioID` (`int`, identity)
  * **Atributos:** `Nome` (`nvarchar(120)`), `Descricao` (`nvarchar(1000)`), `RecompensaXP` (`int`), `InicioEm` (`datetime2(7)`), `FimEm` (`datetime2(7)`), `Ativo` (`bit`), `CriadoEm` (`datetime2(7)`), `TipoRequisito` (`nvarchar(30)`), `MetaRequisito` (`int`), `TrilhaID` (`int`), `ConquistaID` (`int`)

* **`ParticipacoesDesafio`**
  * **PK:** `ParticipacaoID` (`int`, identity)
  * **UK:** (`DesafioID` `int`, `UsuarioID` `int`)
  * **Atributos:** `InscritoEm` (`datetime2(7)`), `ConcluidoEm` (`datetime2(7)`), `Status` (`nvarchar(30)`)

* **`RecompensasConcedidas`**
  * **PK:** `RecompensaID` (`int`, identity)
  * **UK:** (`UsuarioID` `int`, `TipoOrigem` `nvarchar(20)`, `OrigemID` `int`)
  * **Atributos:** `QuantidadeXP` (`int`), `ConquistaID` (`int`), `CriadoEm` (`datetime2(7)`)

---

### 2.5 Assistente Pedagógico (Chat)

* **`ChatConversations`**
  * **PK:** `ConversationId` (`uniqueidentifier`)
  * **UK:** (`ConversationId` `uniqueidentifier`, `UsuarioID` `int`)
  * **Atributos:** `UsuarioID` (`int`), `CriadoEm` (`datetime2(7)`), `AtualizadoEm` (`datetime2(7)`)

* **`ChatMessages`**
  * **PK:** `MessageId` (`uniqueidentifier`)
  * **Atributos:** `ConversationId` (`uniqueidentifier`), `UsuarioID` (`int`), `Role` (`nvarchar(20)`), `Content` (`nvarchar(max)`), `CreatedAt` (`datetime2(7)`)

---

## 3. Relacionamentos e Cardinalidades

* **Usuários, Papéis e Acesso:**
  * `Usuarios` (1) $\leftrightarrow$ (N) `UsuarioPapeis` (N) $\leftrightarrow$ (1) `Papeis`
  * `Usuarios` (1) $\rightarrow$ (N) `RefreshTokens`
  * `Usuarios` (1) $\rightarrow$ (N) `TokensRecuperacaoSenha`
  * `Usuarios` (0..1) $\rightarrow$ (N) `LogsAuditoria` (`ON DELETE SET NULL`)
  * `Usuarios` (0..1) $\rightarrow$ (N) `ConfiguracoesPlataforma` (`ON DELETE SET NULL`)

* **Estrutura Pedagógica e Progresso:**
  * `Materias` (1) $\rightarrow$ (N) `TrilhasAprendizagem` (`ON DELETE CASCADE`)
  * `TrilhasAprendizagem` (1) $\rightarrow$ (N) `Modulos` (`ON DELETE CASCADE`)
  * `Modulos` (1) $\rightarrow$ (N) `Aulas` (`ON DELETE CASCADE`)
  * `Usuarios` (0..1) $\rightarrow$ (N) `TrilhasAprendizagem` (como criador e como aprovador)
  * `Usuarios` (0..1) $\rightarrow$ (N) `Aulas` (como criador e como aprovador)
  * `Usuarios` (1) $\rightarrow$ (N) `RevisoesConteudo`
  * `Usuarios` (1) $\leftrightarrow$ (N) `UsuarioTrilhas` (N) $\leftrightarrow$ (1) `TrilhasAprendizagem`
  * `Usuarios` (1) $\leftrightarrow$ (N) `ProgressoUsuario` (N) $\leftrightarrow$ (1) `Aulas`

* **Questões, Alternativas e Provas:**
  * `Materias` (1) $\rightarrow$ (N) `Questoes` (`ON DELETE CASCADE`)
  * `Modulos` (0..1) $\rightarrow$ (N) `Questoes`
  * `Aulas` (0..1) $\rightarrow$ (N) `Questoes`
  * `TrilhasAprendizagem` (0..1) $\rightarrow$ (N) `Questoes`
  * `Usuarios` (0..1) $\rightarrow$ (N) `Questoes` (como criador e como aprovador)
  * `Questoes` (1) $\rightarrow$ (N) `AlternativasQuestao` (`ON DELETE CASCADE`)
  * `Provas` (1) $\leftrightarrow$ (N) `ProvaQuestoes` (N) $\leftrightarrow$ (1) `Questoes`
  * `Materias` (0..1) $\rightarrow$ (N) `Provas`
  * `Modulos` (0..1) $\rightarrow$ (N) `Provas`
  * `TrilhasAprendizagem` (0..1) $\rightarrow$ (N) `Provas`
  * `Usuarios` (1) $\leftrightarrow$ (N) `ProvasUsuario` (N) $\leftrightarrow$ (1) `Provas`
  * `ProvasUsuario` (1) $\rightarrow$ (N) `RespostasProvasUsuario`
  * `Questoes` (1) $\rightarrow$ (N) `RespostasProvasUsuario`
  * `AlternativasQuestao` (1) $\rightarrow$ (N) `RespostasProvasUsuario` (via chave composta `AlternativaSelecionadaID`, `QuestaoID`)

* **Gamificação:**
  * `Usuarios` (1) $\leftrightarrow$ (1) `XPUsuario`
  * `NiveisGamificacao` (1) $\rightarrow$ (N) `XPUsuario`
  * `Usuarios` (1) $\rightarrow$ (N) `HistoricoXP`
  * `Usuarios` (1) $\rightarrow$ (N) `RecompensasConcedidas`
  * `Usuarios` (1) $\leftrightarrow$ (N) `ConquistasUsuario` (N) $\leftrightarrow$ (1) `Conquistas`
  * `Usuarios` (1) $\leftrightarrow$ (N) `UsuarioConquistas` (N) $\leftrightarrow$ (1) `Conquistas`
  * `TrilhasAprendizagem` (0..1) $\rightarrow$ (N) `Desafios` (`ON DELETE SET NULL`)
  * `Conquistas` (0..1) $\rightarrow$ (N) `Desafios` (`ON DELETE SET NULL`)
  * `Desafios` (1) $\leftrightarrow$ (N) `ParticipacoesDesafio` (N) $\leftrightarrow$ (1) `Usuarios`

* **Conversas do Chat:**
  * `Usuarios` (1) $\rightarrow$ (N) `ChatConversations`
  * `ChatConversations` (1) $\rightarrow$ (N) `ChatMessages` (via chave composta `ConversationId`, `UsuarioID`)