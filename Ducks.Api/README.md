# Ducks API

API em .NET 10 (Clean Architecture: `Ducks.Api`, `Ducks.Application`, `Ducks.Domain`, `Ducks.Infrastructure`) com EF Core e SQL Server/Azure SQL. Visão geral e diagramas: [DocApi.md](DocApi.md) e [DiagramasAPI.md](DiagramasAPI.md).

## Requisitos

- .NET SDK 10
- SQL Server 2019+ ou Azure SQL

## Configuração

Nenhum segredo fica no código. Copie `.env.example` para `.env` (o `.env` está no `.gitignore`) ou use variáveis de ambiente / User Secrets / Key Vault. Variáveis de ambiente já definidas têm prioridade sobre o `.env`.

| Variável | Obrigatória | Padrão | Uso |
| :--- | :--- | :--- | :--- |
| `ConnectionStrings__DefaultConnection` | sim | | Banco |
| `Jwt__Secret` | sim | | Assinatura do JWT (mínimo 32 bytes) |
| `OpenAI__ApiKey`, `OpenAI__Model` | para o Duck Bot | `gpt-6-luna` | Duck Bot |
| `Email__Provedor` | não | `Mock` | `Smtp` envia e-mail de verdade; qualquer outro valor usa o mock (escreve no log) |
| `Email__Remetente`, `Email__NomeRemetente` | com Smtp | `Ducks` | Remetente |
| `Email__Smtp__Host`, `Email__Smtp__Porta`, `Email__Smtp__Usuario`, `Email__Smtp__Senha`, `Email__Smtp__UsarSsl` | com Smtp | porta `587`, SSL `true` | Servidor SMTP |
| `RecuperacaoSenha__UrlRedefinicao` | não | `http://localhost:3000/redefinir-senha` | Página do front que recebe `?token=` |
| `RecuperacaoSenha__ExpiracaoMinutos` | não | `30` (entre 5 e 120) | Validade do link |
| `RateLimiting__Auth__PermitLimit` | não | `10` a cada 5 min por IP | Login e cadastro |
| `RateLimiting__RecuperacaoSenha__PermitLimit` | não | `5` a cada 15 min por IP | Esqueci/redefinir senha |
| `DUCKS_TEST_SQL` | só testes | | Conexão SQL Server para os testes de integração |

## Banco de dados (scripts em `Database/`)

A API não usa migrations do EF: o banco é mantido por scripts SQL.

1. **Banco que já existe (Azure do grupo):** rode só `Database/02_novas_tabelas.sql`. Ele é idempotente: pode ser executado várias vezes, só cria o que falta e nunca apaga dados.
2. **Banco novo, local ou de testes:** rode `Database/01_esquema_base.sql` e depois `Database/02_novas_tabelas.sql`.

```bash
sqlcmd -S localhost -U sa -P "<senha>" -d Ducks -C -i Database/01_esquema_base.sql   # só em banco vazio
sqlcmd -S localhost -U sa -P "<senha>" -d Ducks -C -i Database/02_novas_tabelas.sql
```

No Azure Data Studio / SSMS basta abrir o arquivo e executar no banco certo.

## Executar

```bash
dotnet restore Ducks.slnx
dotnet run --project src/Ducks.Api
```

Em ambiente Development (`ASPNETCORE_ENVIRONMENT=Development`) o Swagger abre na raiz (`/`), já com os comentários das rotas. Saúde: `/api/health`.

## Testes

```bash
# Unitários (banco em memória, não precisam de nada)
dotnet test tests/Ducks.UnitTests

# Integração (sobem a API inteira contra um SQL Server real; cada execução cria e apaga um banco DucksTeste_xxxx)
export DUCKS_TEST_SQL="Server=localhost,1433;User Id=sa;Password=<senha>;TrustServerCertificate=True"
dotnet test Ducks.slnx
```

Sem `DUCKS_TEST_SQL`, os testes de integração aparecem como ignorados (skipped) e os unitários rodam normalmente.

Para ter um SQL Server local rápido:

```bash
docker run -d --name ducksql -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<senha-forte>' -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

## Endpoints acrescentados

| Método | Rota | Papéis | Para quê |
| :--- | :--- | :--- | :--- |
| GET | `/api/admin/content/trilhas/{trilhaId}` | Administrador, Curador | Detalhe da trilha com módulos e aulas (qualquer status) |
| PUT | `/api/admin/content/trilhas/{trilhaId}` | Administrador, Curador | Edita trilha (volta para RASCUNHO) |
| POST | `/api/admin/content/trilhas/{trilhaId}/enviar-revisao` | Administrador, Curador | Envia trilha para revisão (PENDENTE) |
| GET | `/api/admin/content/trilhas/{trilhaId}/modulos` | Administrador, Curador | Lista módulos (paginação, filtro por status, ordenação) |
| POST | `/api/admin/content/trilhas/{trilhaId}/modulos` | Administrador, Curador | Cria módulo |
| PUT | `/api/admin/content/trilhas/{trilhaId}/modulos/ordem` | Administrador, Curador | Reordena módulos |
| GET | `/api/admin/content/modulos/{moduloId}` | Administrador, Curador | Detalhe do módulo |
| PUT | `/api/admin/content/modulos/{moduloId}` | Administrador, Curador | Edita módulo |
| DELETE | `/api/admin/content/modulos/{moduloId}` | Administrador, Curador | Exclui o módulo de vez, com as aulas e o progresso delas (para só tirar do ar, use a remoção da moderação) |
| POST | `/api/admin/content/modulos/{moduloId}/enviar-revisao` | Administrador, Curador | Envia módulo para revisão |
| GET | `/api/admin/content/modulos/{moduloId}/aulas` | Administrador, Curador | Lista aulas do módulo |
| POST | `/api/admin/content/modulos/{moduloId}/aulas` | Administrador, Curador | Cria aula |
| PUT | `/api/admin/content/modulos/{moduloId}/aulas/ordem` | Administrador, Curador | Reordena aulas |
| GET | `/api/admin/content/aulas/{aulaId}` | Administrador, Curador | Detalhe da aula |
| PUT | `/api/admin/content/aulas/{aulaId}` | Administrador, Curador | Edita aula |
| DELETE | `/api/admin/content/aulas/{aulaId}` | Administrador, Curador | Exclui a aula de vez, com o progresso dela |
| POST | `/api/admin/content/aulas/{aulaId}/enviar-revisao` | Administrador, Curador | Envia aula para revisão |
| GET | `/api/admin/content/questoes` | Administrador, Curador | Banco de questões (filtros por matéria, dificuldade, tipo, status, texto) |
| GET | `/api/admin/content/questoes/{questaoId}` | Administrador, Curador | Questão com gabarito e explicações |
| POST | `/api/admin/content/questoes` | Administrador, Curador | Cria questão com alternativas |
| PUT | `/api/admin/content/questoes/{questaoId}` | Administrador, Curador | Edita questão |
| DELETE | `/api/admin/content/questoes/{questaoId}` | Administrador, Curador | Exclui questão (só se nunca respondida) |
| POST | `/api/admin/content/questoes/{questaoId}/enviar-revisao` | Administrador, Curador | Envia questão para revisão |
| GET | `/api/admin/content/simulados` | Administrador, Curador | Lista simulados |
| GET | `/api/admin/content/simulados/{provaId}` | Administrador, Curador | Detalhe do simulado com questões e gabarito |
| POST | `/api/admin/content/simulados` | Administrador, Curador | Cria simulado (nota mínima, tempo, tentativas, XP) |
| PUT | `/api/admin/content/simulados/{provaId}` | Administrador, Curador | Edita simulado |
| DELETE | `/api/admin/content/simulados/{provaId}` | Administrador, Curador | Exclui simulado (só sem tentativas) |
| PUT | `/api/admin/content/simulados/{provaId}/questoes` | Administrador, Curador | Define questões manualmente |
| POST | `/api/admin/content/simulados/{provaId}/questoes/sortear` | Administrador, Curador | Sorteia questões aprovadas por filtro |
| POST | `/api/admin/content/simulados/{provaId}/enviar-revisao` | Administrador, Curador | Envia simulado para revisão |
| GET | `/api/moderacao/pendentes` | Administrador, Moderador | Fila de itens PENDENTE (todos os tipos) |
| GET | `/api/moderacao/historico` | Administrador, Moderador | Histórico de revisões (filtro por tipo, item, ação) |
| GET | `/api/moderacao/{tipo}/{id}` | Administrador, Moderador | Item para revisão + histórico dele (`tipo` = trilha, modulo, aula, questao, simulado) |
| POST | `/api/moderacao/{tipo}/{id}/aprovar` | Administrador, Moderador | Aprova (comentário opcional) |
| POST | `/api/moderacao/{tipo}/{id}/rejeitar` | Administrador, Moderador | Rejeita (justificativa obrigatória) |
| POST | `/api/moderacao/{tipo}/{id}/remover` | Administrador, Moderador | Remove do catálogo (justificativa obrigatória) |
| GET | `/api/educacional/trilhas/{trilhaId}` | Qualquer usuário logado | Trilha aprovada com módulos e aulas aprovados |
| GET | `/api/educacional/trilhas/{trilhaId}/modulos` | Qualquer usuário logado | Módulos aprovados |
| GET | `/api/educacional/modulos/{moduloId}` | Qualquer usuário logado | Módulo aprovado |
| GET | `/api/educacional/modulos/{moduloId}/aulas` | Qualquer usuário logado | Aulas aprovadas |
| GET | `/api/educacional/aulas/{aulaId}` | Qualquer usuário logado | Aula aprovada (com conteúdo) |
| POST | `/api/educacional/trilhas/{trilhaId}/matricula` | Qualquer usuário logado | Matricula na trilha (idempotente) |
| DELETE | `/api/educacional/trilhas/{trilhaId}/matricula` | Qualquer usuário logado | Cancela matrícula (progresso é mantido) |
| GET | `/api/educacional/matriculas` | Qualquer usuário logado | Minhas matrículas com percentual |
| POST | `/api/educacional/aulas/{aulaId}/concluir` | Qualquer usuário logado | Conclui aula (XP uma única vez; fecha a trilha ao chegar a 100%) |
| GET | `/api/educacional/trilhas/{trilhaId}/progresso` | Qualquer usuário logado | Progresso detalhado por módulo/aula |
| GET | `/api/educacional/historico` | Qualquer usuário logado | Histórico de aulas, simulados e recompensas |
| GET | `/api/educacional/dashboard` | Qualquer usuário logado | Resumo: XP, nível, trilhas, médias, conquistas |
| GET | `/api/educacional/simulados` | Qualquer usuário logado | Simulados aprovados (sem gabarito) |
| GET | `/api/educacional/simulados/{provaId}` | Qualquer usuário logado | Detalhe do simulado (sem gabarito) |
| POST | `/api/educacional/simulados/{provaId}/tentativas` | Qualquer usuário logado | Inicia tentativa (respeita limite; retoma a que estiver aberta) |
| GET | `/api/educacional/tentativas` | Qualquer usuário logado | Minhas tentativas |
| GET | `/api/educacional/tentativas/{tentativaId}` | Dono da tentativa | Tentativa; feedback e gabarito só depois de finalizada |
| PUT | `/api/educacional/tentativas/{tentativaId}/respostas` | Dono da tentativa | Salva respostas parciais (dentro do tempo) |
| POST | `/api/educacional/tentativas/{tentativaId}/finalizar` | Dono da tentativa | Corrige, dá nota, feedback e XP (não pode reenviar) |
| GET | `/api/educacional/desafios` | Qualquer usuário logado | Desafios ativos com minha situação |
| POST | `/api/educacional/desafios/{desafioId}/participar` | Qualquer usuário logado | Inscreve no desafio (idempotente) |
| GET | `/api/educacional/desafios/{desafioId}/progresso` | Qualquer usuário logado | Progresso no requisito |
| POST | `/api/educacional/desafios/{desafioId}/concluir` | Qualquer usuário logado | Conclui se a meta foi atingida (XP e medalha uma única vez) |
| POST | `/api/auth/esqueci-senha` | Público (limite 5 por IP a cada 15 min) | Pede o link; resposta sempre igual (202) |
| POST | `/api/auth/redefinir-senha` | Público (mesmo limite) | Troca a senha com o token; invalida sessões antigas |

Rotas já existentes que receberam campos novos (sem mudar a rota): `POST/PUT /api/admin/gamification/challenges` aceitam `tipoRequisito`, `metaRequisito`, `trilhaId` e `conquistaId` (opcionais).
