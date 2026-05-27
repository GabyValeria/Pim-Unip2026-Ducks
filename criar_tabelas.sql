USE DucksBD;
GO

/* =========================================================
   PERFIL
========================================================= */

CREATE TABLE Perfil (
    id_perfil INT PRIMARY KEY IDENTITY(1,1),
    nome VARCHAR(50) NOT NULL,
    descricao TEXT
);

/* =========================================================
   USUÁRIO
========================================================= */

CREATE TABLE Usuario (
    id_usuario INT PRIMARY KEY IDENTITY(1,1),

    nome VARCHAR(150) NOT NULL,
    email VARCHAR(150) UNIQUE NOT NULL,
    senha_hash VARCHAR(255) NOT NULL,

    foto_perfil VARCHAR(255),

    data_nascimento DATE,

    data_cadastro DATETIME DEFAULT GETDATE(),

    status_usuario VARCHAR(20)
    CHECK (status_usuario IN (
        'ATIVO',
        'INATIVO',
        'BLOQUEADO'
    )),

    xp_total INT DEFAULT 0,

    nivel_usuario INT DEFAULT 1,

    id_perfil INT NOT NULL,

    FOREIGN KEY (id_perfil)
        REFERENCES Perfil(id_perfil)
);

/* =========================================================
   CATEGORIA
========================================================= */

CREATE TABLE Categoria (
    id_categoria INT PRIMARY KEY IDENTITY(1,1),

    nome VARCHAR(100) NOT NULL,

    descricao TEXT
);

/* =========================================================
   CURSO
========================================================= */

CREATE TABLE Curso (
    id_curso INT PRIMARY KEY IDENTITY(1,1),

    titulo VARCHAR(150) NOT NULL,

    descricao TEXT,

    carga_horaria INT,

    status_curso VARCHAR(20)
    CHECK (status_curso IN (
        'ATIVO',
        'INATIVO'
    )),

    criado_em DATETIME DEFAULT GETDATE(),

    id_categoria INT,

    FOREIGN KEY (id_categoria)
        REFERENCES Categoria(id_categoria)
);

/* =========================================================
   TRILHA
========================================================= */

CREATE TABLE Trilha (
    id_trilha INT PRIMARY KEY IDENTITY(1,1),

    titulo VARCHAR(150) NOT NULL,

    descricao TEXT,

    nivel VARCHAR(20)
    CHECK (nivel IN (
        'BASICO',
        'INTERMEDIARIO',
        'AVANCADO'
    )),

    status_trilha VARCHAR(20)
    CHECK (status_trilha IN (
        'RASCUNHO',
        'PUBLICADA',
        'DESATIVADA'
    )),

    criado_em DATETIME DEFAULT GETDATE(),

    id_curso INT,

    criado_por INT,

    FOREIGN KEY (id_curso)
        REFERENCES Curso(id_curso),

    FOREIGN KEY (criado_por)
        REFERENCES Usuario(id_usuario)
);

/* =========================================================
   CONTEÚDO
========================================================= */

CREATE TABLE Conteudo (
    id_conteudo INT PRIMARY KEY IDENTITY(1,1),

    titulo VARCHAR(150) NOT NULL,

    descricao TEXT,

    tipo VARCHAR(20)
    CHECK (tipo IN (
        'VIDEO',
        'PDF',
        'QUIZ',
        'ARTIGO',
        'LINK'
    )),

    url_conteudo VARCHAR(255),

    ordem_conteudo INT,

    criado_em DATETIME DEFAULT GETDATE(),

    id_trilha INT,

    FOREIGN KEY (id_trilha)
        REFERENCES Trilha(id_trilha)
);

/* =========================================================
   ATIVIDADE
========================================================= */

CREATE TABLE Atividade (
    id_atividade INT PRIMARY KEY IDENTITY(1,1),

    titulo VARCHAR(150) NOT NULL,

    descricao NVARCHAR(MAX),

    tipo_atividade VARCHAR(30)
    CHECK (tipo_atividade IN (
        'QUIZ',
        'EXERCICIO',
        'SIMULADO',
        'PROVA',
        'DESAFIO'
    )),

    dificuldade VARCHAR(20)
    CHECK (dificuldade IN (
        'FACIL',
        'MEDIA',
        'DIFICIL'
    )),

    xp_recompensa INT DEFAULT 0,

    moedas_recompensa INT DEFAULT 0,

    tempo_limite INT,

    status_atividade VARCHAR(20)
    CHECK (status_atividade IN (
        'RASCUNHO',
        'PUBLICADA',
        'ARQUIVADA'
    )),

    criado_em DATETIME DEFAULT GETDATE(),

    id_trilha INT NOT NULL,

    FOREIGN KEY (id_trilha)
        REFERENCES Trilha(id_trilha)
);

/* =========================================================
   AVALIAÇÃO
========================================================= */

CREATE TABLE Avaliacao (
    id_avaliacao INT PRIMARY KEY IDENTITY(1,1),

    titulo VARCHAR(150) NOT NULL,

    descricao TEXT,

    nota_maxima DECIMAL(5,2),

    tempo_limite INT,

    tentativas_permitidas INT DEFAULT 1,

    criado_em DATETIME DEFAULT GETDATE(),

    id_trilha INT,

    FOREIGN KEY (id_trilha)
        REFERENCES Trilha(id_trilha)
);

/* =========================================================
   PERGUNTA
========================================================= */

CREATE TABLE Pergunta (
    id_pergunta INT PRIMARY KEY IDENTITY(1,1),

    enunciado NVARCHAR(MAX) NOT NULL,

    explicacao_geral NVARCHAR(MAX),

    tipo VARCHAR(30)
    CHECK (tipo IN (
        'MULTIPLA_ESCOLHA',
        'VERDADEIRO_FALSO',
        'DISSERTATIVA'
    )),

    dificuldade VARCHAR(20)
    CHECK (dificuldade IN (
        'FACIL',
        'MEDIA',
        'DIFICIL'
    )),

    pontos INT DEFAULT 1,

    id_avaliacao INT,

    id_atividade INT,

    FOREIGN KEY (id_avaliacao)
        REFERENCES Avaliacao(id_avaliacao),

    FOREIGN KEY (id_atividade)
        REFERENCES Atividade(id_atividade)
);

/* =========================================================
   ALTERNATIVA
========================================================= */

CREATE TABLE Alternativa (
    id_alternativa INT PRIMARY KEY IDENTITY(1,1),

    texto NVARCHAR(MAX) NOT NULL,

    correta BIT DEFAULT 0,

    explicacao NVARCHAR(MAX),

    ordem_alternativa INT DEFAULT 1,

    id_pergunta INT,

    FOREIGN KEY (id_pergunta)
        REFERENCES Pergunta(id_pergunta)
);

/* =========================================================
   TENTATIVA AVALIAÇÃO
========================================================= */

CREATE TABLE TentativaAvaliacao (
    id_tentativa INT PRIMARY KEY IDENTITY(1,1),

    nota DECIMAL(5,2),

    data_inicio DATETIME,

    data_fim DATETIME,

    status_tentativa VARCHAR(20)
    CHECK (status_tentativa IN (
        'EM_ANDAMENTO',
        'FINALIZADA'
    )),

    id_usuario INT,

    id_avaliacao INT,

    FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario),

    FOREIGN KEY (id_avaliacao)
        REFERENCES Avaliacao(id_avaliacao)
);

/* =========================================================
   RESPOSTA USUÁRIO
========================================================= */

CREATE TABLE RespostaUsuario (
    id_resposta INT PRIMARY KEY IDENTITY(1,1),

    correta BIT,

    respondido_em DATETIME DEFAULT GETDATE(),

    id_tentativa INT,

    id_pergunta INT,

    id_alternativa INT,

    FOREIGN KEY (id_tentativa)
        REFERENCES TentativaAvaliacao(id_tentativa),

    FOREIGN KEY (id_pergunta)
        REFERENCES Pergunta(id_pergunta),

    FOREIGN KEY (id_alternativa)
        REFERENCES Alternativa(id_alternativa)
);

/* =========================================================
   FEEDBACK RESPOSTA
========================================================= */

CREATE TABLE FeedbackResposta (
    id_feedback INT PRIMARY KEY IDENTITY(1,1),

    feedback NVARCHAR(MAX),

    recomendacao NVARCHAR(MAX),

    criado_em DATETIME DEFAULT GETDATE(),

    id_resposta INT NOT NULL,

    FOREIGN KEY (id_resposta)
        REFERENCES RespostaUsuario(id_resposta)
);

/* =========================================================
   PROGRESSO
========================================================= */

CREATE TABLE Progresso (
    id_progresso INT PRIMARY KEY IDENTITY(1,1),

    porcentagem DECIMAL(5,2),

    ultima_atividade DATETIME,

    status_progresso VARCHAR(20)
    CHECK (status_progresso IN (
        'NAO_INICIADO',
        'EM_ANDAMENTO',
        'CONCLUIDO'
    )),

    id_usuario INT,

    id_trilha INT,

    FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario),

    FOREIGN KEY (id_trilha)
        REFERENCES Trilha(id_trilha)
);

/* =========================================================
   GAMIFICAÇÃO
========================================================= */

CREATE TABLE Gamificacao (
    id_gamificacao INT PRIMARY KEY IDENTITY(1,1),

    xp_ganho INT,

    moedas INT DEFAULT 0,

    acao VARCHAR(150),

    data_evento DATETIME DEFAULT GETDATE(),

    id_usuario INT,

    FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario)
);

/* =========================================================
   BADGE
========================================================= */

CREATE TABLE Badge (
    id_badge INT PRIMARY KEY IDENTITY(1,1),

    nome VARCHAR(100),

    descricao TEXT,

    icone VARCHAR(255),

    xp_necessario INT
);

/* =========================================================
   USUÁRIO BADGE
========================================================= */

CREATE TABLE UsuarioBadge (
    id_usuario_badge INT PRIMARY KEY IDENTITY(1,1),

    conquistado_em DATETIME DEFAULT GETDATE(),

    id_usuario INT,

    id_badge INT,

    FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario),

    FOREIGN KEY (id_badge)
        REFERENCES Badge(id_badge)
);

/* =========================================================
   RELATÓRIO
========================================================= */

CREATE TABLE Relatorio (
    id_relatorio INT PRIMARY KEY IDENTITY(1,1),

    tipo_relatorio VARCHAR(100),

    dados NVARCHAR(MAX),

    gerado_em DATETIME DEFAULT GETDATE(),

    id_usuario INT,

    FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario)
);

/* =========================================================
   ANALYTICS USUÁRIO
========================================================= */

CREATE TABLE AnalyticsUsuario (
    id_analytics INT PRIMARY KEY IDENTITY(1,1),

    tempo_total_estudo INT DEFAULT 0,

    total_atividades INT DEFAULT 0,

    total_acertos INT DEFAULT 0,

    total_erros INT DEFAULT 0,

    media_notas DECIMAL(5,2),

    sequencia_dias INT DEFAULT 0,

    ultima_atividade DATETIME,

    nivel_desempenho VARCHAR(20)
    CHECK (nivel_desempenho IN (
        'BAIXO',
        'MEDIO',
        'ALTO'
    )),

    id_usuario INT UNIQUE NOT NULL,

    FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario)
);

/* =========================================================
   HISTÓRICO EDUCACIONAL
========================================================= */

CREATE TABLE HistoricoEducacional (
    id_historico INT PRIMARY KEY IDENTITY(1,1),

    data_evento DATETIME DEFAULT GETDATE(),

    tipo_evento VARCHAR(50),

    descricao NVARCHAR(MAX),

    xp_ganho INT DEFAULT 0,

    id_usuario INT NOT NULL,

    id_trilha INT NULL,

    id_atividade INT NULL,

    FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario),

    FOREIGN KEY (id_trilha)
        REFERENCES Trilha(id_trilha),

    FOREIGN KEY (id_atividade)
        REFERENCES Atividade(id_atividade)
);

/* =========================================================
   MISSÃO
========================================================= */

CREATE TABLE Missao (
    id_missao INT PRIMARY KEY IDENTITY(1,1),

    titulo VARCHAR(150),

    descricao NVARCHAR(MAX),

    objetivo INT NOT NULL,

    xp_recompensa INT DEFAULT 0,

    moedas_recompensa INT DEFAULT 0,

    tipo_missao VARCHAR(30)
    CHECK (tipo_missao IN (
        'DIARIA',
        'SEMANAL',
        'ESPECIAL'
    )),

    ativa BIT DEFAULT 1
);

/* =========================================================
   USUÁRIO MISSÃO
========================================================= */

CREATE TABLE UsuarioMissao (
    id_usuario_missao INT PRIMARY KEY IDENTITY(1,1),

    progresso INT DEFAULT 0,

    concluida BIT DEFAULT 0,

    concluida_em DATETIME,

    id_usuario INT NOT NULL,

    id_missao INT NOT NULL,

    FOREIGN KEY (id_usuario)
        REFERENCES Usuario(id_usuario),

    FOREIGN KEY (id_missao)
        REFERENCES Missao(id_missao)
);