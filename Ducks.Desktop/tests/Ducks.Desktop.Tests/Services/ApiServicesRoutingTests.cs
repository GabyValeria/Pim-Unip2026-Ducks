using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Services;
using Ducks.Desktop.Core.Models;
using Moq;

namespace Ducks.Desktop.Tests
{
    /// <summary>Verifica que cada serviço chama a rota/verbo corretos do IApiClient e trata respostas vazias.</summary>
    public class ApiServicesRoutingTests
    {
        private readonly Mock<IApiClient> _api = new();

        // ---------------- Usuários e papéis ----------------

        [Fact]
        public async Task UserAdmin_GetUsers_BuildsPagedUrlAndReturnsResult()
        {
            var esperado = new PagedResult<UserDto> { Page = 2, PageSize = 10, Total = 11 };
            _api.Setup(a => a.GetAsync<PagedResult<UserDto>>("api/admin/users?page=2&pageSize=10&search=ana", It.IsAny<CancellationToken>()))
                .ReturnsAsync(esperado);

            var result = await new UserAdminApiService(_api.Object).GetUsersAsync("ana", 2, 10);

            Assert.Same(esperado, result);
        }

        [Fact]
        public async Task UserAdmin_GetUsers_WhenApiReturnsNothing_ReturnsEmptyPage()
        {
            var result = await new UserAdminApiService(_api.Object).GetUsersAsync(null, 3, 25);

            Assert.Equal(3, result.Page);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task UserAdmin_SetBlocked_SendsPutToBlockRoute()
        {
            await new UserAdminApiService(_api.Object).SetBlockedAsync(5, true);

            _api.Verify(a => a.PutAsync("api/admin/users/5/block?block=true", null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Role_Assign_PostsUserAndRoleIds()
        {
            await new RoleApiService(_api.Object).AssignRoleAsync(7, 2);

            _api.Verify(a => a.PostAsync("api/admin/roles/assign",
                It.Is<AssignRoleRequestDto>(d => d.UsuarioID == 7 && d.PapelID == 2),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Role_GetRoles_WhenApiReturnsNothing_ReturnsEmptyList()
        {
            var result = await new RoleApiService(_api.Object).GetRolesAsync();

            Assert.Empty(result);
        }

        // ---------------- Métricas, auditoria, configurações, IA ----------------

        [Fact]
        public async Task Metrics_WhenApiReturnsNothing_ReturnsZeroedMetrics()
        {
            var result = await new MetricsApiService(_api.Object).GetDashboardMetricsAsync();

            Assert.Equal(0, result.TotalUsuarios);
            Assert.Equal(0, result.PercentualAtivos);
        }

        [Fact]
        public async Task Settings_Save_PutsKeyAndValue()
        {
            await new SettingsApiService(_api.Object).SaveSettingAsync("site.nome", "Ducks");

            _api.Verify(a => a.PutAsync("api/admin/settings",
                It.Is<UpdateSettingRequestDto>(d => d.Chave == "site.nome" && d.Valor == "Ducks"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Audit_GetLogs_UsesAuditRoute()
        {
            var logs = new List<AuditLogDto> { new() { LogID = 1, Acao = "LOGIN" } };
            _api.Setup(a => a.GetAsync<List<AuditLogDto>>("api/admin/audit/logs", It.IsAny<CancellationToken>())).ReturnsAsync(logs);

            var result = await new AuditApiService(_api.Object).GetLogsAsync();

            Assert.Single(result);
        }

        [Fact]
        public async Task Ai_WhenApiReturnsNothing_ReturnsFailureResponse()
        {
            var result = await new AiAutomationApiService(_api.Object)
                .ProcessarTarefaAdminAsync(new AiPromptRequestDto { Contexto = "x", TarefaAdmin = "y" });

            Assert.False(result.Sucesso);
            Assert.False(string.IsNullOrWhiteSpace(result.MensagemErro));
        }

        // ---------------- Gamificação ----------------

        [Fact]
        public async Task Gamification_BadgeCrud_UsesExpectedVerbsAndRoutes()
        {
            var service = new GamificationApiService(_api.Object);
            var request = new BadgeRequestDto { Nome = "Pato Mestre", XPNecessario = 100 };

            await service.CreateBadgeAsync(request);
            await service.UpdateBadgeAsync(3, request);
            await service.DeleteBadgeAsync(3);

            _api.Verify(a => a.PostAsync("api/admin/gamification/badges", request, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PutAsync("api/admin/gamification/badges/3", request, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.DeleteAsync("api/admin/gamification/badges/3", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Gamification_ChallengeCrud_UsesExpectedVerbsAndRoutes()
        {
            var service = new GamificationApiService(_api.Object);
            var request = new ChallengeRequestDto { Nome = "Maratona" };

            await service.CreateChallengeAsync(request);
            await service.UpdateChallengeAsync(4, request);
            await service.DeleteChallengeAsync(4);

            _api.Verify(a => a.PostAsync("api/admin/gamification/challenges", request, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PutAsync("api/admin/gamification/challenges/4", request, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.DeleteAsync("api/admin/gamification/challenges/4", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Gamification_UserBadges_UsesUserRoute()
        {
            _api.Setup(a => a.GetAsync<List<UserBadgeDto>>("api/admin/gamification/users/9/badges", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<UserBadgeDto> { new() { ConquistaID = 1, Nome = "Primeira aula" } });

            var result = await new GamificationApiService(_api.Object).GetUserBadgesAsync(9);

            Assert.Single(result);
        }

        // ---------------- Conteúdo ----------------

        [Fact]
        public async Task Content_Operations_UseExpectedRoutes()
        {
            var service = new ContentApiService(_api.Object);
            var trilha = new CreateTrilhaRequestDto { NomeTrilha = "Matemática", MateriaID = 1 };

            await service.CreateTrilhaAsync(trilha);
            await service.DeleteTrilhaAsync(7);
            await service.SendTrilhaToReviewAsync(7);

            _api.Verify(a => a.PostAsync("api/admin/content/trilhas", trilha, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.DeleteAsync("api/admin/content/trilhas/7", It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PostAsync("api/admin/content/trilhas/7/enviar-revisao", null, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---------------- Moderação ----------------

        [Fact]
        public async Task Moderation_Reject_PostsJustificationToRejectRoute()
        {
            var service = new ModerationApiService(_api.Object, new Mock<IHttpClientFactory>().Object, new Mock<Ducks.Desktop.Application.Interfaces.IAuthApiService>().Object);

            await service.RejectAsync("AULA", 5, "Conteúdo impróprio");

            _api.Verify(a => a.PostAsync("api/moderacao/AULA/5/rejeitar", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Moderation_Approve_PostsToApproveRoute()
        {
            var service = new ModerationApiService(_api.Object, new Mock<IHttpClientFactory>().Object, new Mock<Ducks.Desktop.Application.Interfaces.IAuthApiService>().Object);

            await service.ApproveAsync("TRILHA", 3);

            _api.Verify(a => a.PostAsync("api/moderacao/TRILHA/3/aprovar", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Moderation_GetPending_ParsesEnvelopeReturnedByApi()
        {
            var json = System.Text.Json.JsonDocument.Parse("{\"items\":[{\"tipo\":\"TRILHA\",\"conteudoId\":3,\"titulo\":\"Álgebra\"}]}").RootElement.Clone();
            _api.Setup(a => a.GetAsync<System.Text.Json.JsonElement>("api/moderacao/pendentes", It.IsAny<CancellationToken>()))
                .ReturnsAsync(json);
            var service = new ModerationApiService(_api.Object, new Mock<IHttpClientFactory>().Object, new Mock<Ducks.Desktop.Application.Interfaces.IAuthApiService>().Object);

            var result = await service.GetPendingAsync();

            var item = Assert.Single(result);
            Assert.Equal("TRILHA", item.Tipo);
            Assert.Equal(3, item.ConteudoID);
            Assert.Equal("Álgebra", item.Titulo);
        }
    }
}
