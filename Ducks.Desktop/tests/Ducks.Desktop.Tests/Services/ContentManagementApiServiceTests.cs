using Ducks.Desktop.Application.Http;
using Ducks.Desktop.Application.Services;
using Ducks.Desktop.Core.Models;
using Moq;

namespace Ducks.Desktop.Tests
{
    /// <summary>Rotas e verbos dos serviços de módulos/aulas, questões/simulados e XP.</summary>
    public class ContentManagementApiServiceTests
    {
        private readonly Mock<IApiClient> _api = new();

        // ---------------- Módulos e aulas ----------------

        [Fact]
        public async Task Modulos_GetUsesPagedRouteAndReturnsItems()
        {
            _api.Setup(a => a.GetAsync<PagedResult<ModuloDto>>("api/admin/content/trilhas/3/modulos?page=1&pageSize=100", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<ModuloDto> { Items = { new ModuloDto { ModuloID = 9, Titulo = "Frações" } } });

            var result = await new ContentManagementApiService(_api.Object).GetModulosAsync(3);

            Assert.Single(result);
            Assert.Equal(9, result[0].ModuloID);
        }

        [Fact]
        public async Task Modulos_GetWhenApiReturnsNothing_ReturnsEmptyList()
        {
            var result = await new ContentManagementApiService(_api.Object).GetModulosAsync(3);

            Assert.Empty(result);
        }

        [Fact]
        public async Task Modulos_Operations_UseExpectedRoutes()
        {
            var service = new ContentManagementApiService(_api.Object);
            var dto = new ModuloRequestDto { Titulo = "Frações" };

            await service.CreateModuloAsync(3, dto);
            await service.UpdateModuloAsync(9, dto);
            await service.DeleteModuloAsync(9);
            await service.SendModuloToReviewAsync(9);
            await service.ReorderModulosAsync(3, new[] { 5, 9, 2 });

            _api.Verify(a => a.PostAsync("api/admin/content/trilhas/3/modulos", dto, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PutAsync("api/admin/content/modulos/9", dto, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.DeleteAsync("api/admin/content/modulos/9", It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PostAsync("api/admin/content/modulos/9/enviar-revisao", null, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PutAsync("api/admin/content/trilhas/3/modulos/ordem",
                It.Is<ReordenarRequestDto>(r => r.Ids.SequenceEqual(new[] { 5, 9, 2 })), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Aulas_Operations_UseExpectedRoutes()
        {
            var service = new ContentManagementApiService(_api.Object);
            var dto = new AulaRequestDto { Titulo = "Introdução", UrlConteudo = "https://exemplo.com/v1" };

            await service.GetAulasAsync(9);
            await service.CreateAulaAsync(9, dto);
            await service.UpdateAulaAsync(4, dto);
            await service.DeleteAulaAsync(4);
            await service.SendAulaToReviewAsync(4);
            await service.ReorderAulasAsync(9, new[] { 4, 1 });

            _api.Verify(a => a.GetAsync<PagedResult<AulaDto>>("api/admin/content/modulos/9/aulas?page=1&pageSize=100", It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PostAsync("api/admin/content/modulos/9/aulas", dto, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PutAsync("api/admin/content/aulas/4", dto, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.DeleteAsync("api/admin/content/aulas/4", It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PostAsync("api/admin/content/aulas/4/enviar-revisao", null, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PutAsync("api/admin/content/modulos/9/aulas/ordem",
                It.Is<ReordenarRequestDto>(r => r.Ids.SequenceEqual(new[] { 4, 1 })), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Trilha_Update_SendsPutToTrilhaRoute()
        {
            var dto = new CreateTrilhaRequestDto { NomeTrilha = "Matemática", MateriaID = 1 };

            await new ContentManagementApiService(_api.Object).UpdateTrilhaAsync(7, dto);

            _api.Verify(a => a.PutAsync("api/admin/content/trilhas/7", dto, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---------------- Questões ----------------

        [Fact]
        public async Task Questoes_GetBuildsFilteredPagedUrl()
        {
            var esperado = new PagedResult<QuestaoDto> { Page = 2, PageSize = 25, Total = 30 };
            _api.Setup(a => a.GetAsync<PagedResult<QuestaoDto>>("api/admin/content/questoes?page=2&pageSize=25&busca=fra%C3%A7%C3%A3o&status=APROVADO&dificuldade=FACIL", It.IsAny<CancellationToken>()))
                .ReturnsAsync(esperado);

            var result = await new AssessmentApiService(_api.Object).GetQuestoesAsync("fração", "APROVADO", "FACIL", 2, 25);

            Assert.Same(esperado, result);
        }

        [Fact]
        public async Task Questoes_GetWithoutFilters_OmitsOptionalParameters()
        {
            await new AssessmentApiService(_api.Object).GetQuestoesAsync(null, null, null, 1, 25);

            _api.Verify(a => a.GetAsync<PagedResult<QuestaoDto>>("api/admin/content/questoes?page=1&pageSize=25", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Questoes_Operations_UseExpectedRoutes()
        {
            var service = new AssessmentApiService(_api.Object);
            var dto = new QuestaoRequestDto { Enunciado = "Quanto é 2+2?" };

            await service.CreateQuestaoAsync(dto);
            await service.UpdateQuestaoAsync(8, dto);
            await service.DeleteQuestaoAsync(8);
            await service.SendQuestaoToReviewAsync(8);

            _api.Verify(a => a.PostAsync("api/admin/content/questoes", dto, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PutAsync("api/admin/content/questoes/8", dto, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.DeleteAsync("api/admin/content/questoes/8", It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PostAsync("api/admin/content/questoes/8/enviar-revisao", null, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---------------- Simulados ----------------

        [Fact]
        public async Task Simulados_Operations_UseExpectedRoutes()
        {
            var service = new AssessmentApiService(_api.Object);
            var dto = new SimuladoRequestDto { Titulo = "Simulado 1" };

            await service.GetSimuladosAsync("geral", "RASCUNHO", 1, 25);
            await service.GetSimuladoAsync(5);
            await service.CreateSimuladoAsync(dto);
            await service.UpdateSimuladoAsync(5, dto);
            await service.DeleteSimuladoAsync(5);
            await service.SendSimuladoToReviewAsync(5);

            _api.Verify(a => a.GetAsync<PagedResult<SimuladoDto>>("api/admin/content/simulados?page=1&pageSize=25&status=RASCUNHO&busca=geral", It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.GetAsync<SimuladoDto>("api/admin/content/simulados/5", It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PostAsync("api/admin/content/simulados", dto, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PutAsync("api/admin/content/simulados/5", dto, It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.DeleteAsync("api/admin/content/simulados/5", It.IsAny<CancellationToken>()), Times.Once);
            _api.Verify(a => a.PostAsync("api/admin/content/simulados/5/enviar-revisao", null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Simulados_SetQuestoes_SendsOrderedIdsAndReturnsUpdatedSimulado()
        {
            var atualizado = new SimuladoDto { ProvaID = 5 };
            _api.Setup(a => a.PutAsync<SimuladoDto>("api/admin/content/simulados/5/questoes",
                    It.Is<DefinirQuestoesRequestDto>(d => d.QuestaoIds.SequenceEqual(new[] { 3, 1, 2 })), It.IsAny<CancellationToken>()))
                .ReturnsAsync(atualizado);

            var result = await new AssessmentApiService(_api.Object).SetQuestoesAsync(5, new[] { 3, 1, 2 });

            Assert.Same(atualizado, result);
        }

        [Fact]
        public async Task Simulados_Sortear_PostsFiltersToSortRoute()
        {
            var filtros = new SortearQuestoesRequestDto { Quantidade = 5, Dificuldade = "MEDIO" };

            await new AssessmentApiService(_api.Object).SortearQuestoesAsync(5, filtros);

            _api.Verify(a => a.PostAsync<SimuladoDto>("api/admin/content/simulados/5/questoes/sortear", filtros, It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---------------- XP ----------------

        [Fact]
        public async Task Xp_Adjust_PostsToAddRoute()
        {
            var dto = new AdicionarXpRequestDto { UsuarioID = 4, QuantidadeXP = -50, Motivo = "Correção" };

            await new XpApiService(_api.Object).AdjustXpAsync(dto);

            _api.Verify(a => a.PostAsync("api/educacional/xp/adicionar", dto, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
