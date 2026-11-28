using Ducks.Desktop.Application.ViewModels.Forms;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Tests
{
    public class ModelsTests
    {
        [Theory]
        [InlineData(0, 25, 1)]
        [InlineData(25, 25, 1)]
        [InlineData(26, 25, 2)]
        [InlineData(100, 25, 4)]
        [InlineData(10, 0, 1)]
        public void PagedResult_TotalPages(int total, int pageSize, int expected)
        {
            var paged = new PagedResult<int> { Total = total, PageSize = pageSize };

            Assert.Equal(expected, paged.TotalPages);
        }

        [Fact]
        public void UserDto_DisplayHelpers()
        {
            var user = new UserDto { Roles = new[] { "Admin", "Curador" }, IsBlocked = true };

            Assert.Equal("Admin, Curador", user.RolesDisplay);
            Assert.Equal("Bloqueado", user.StatusDisplay);
            Assert.True(user.HasRole("admin"));
            Assert.False(user.HasRole("Moderador"));
        }

        [Fact]
        public void UserDto_WithoutRoles_ShowsDash()
        {
            var user = new UserDto();

            Assert.Equal("—", user.RolesDisplay);
            Assert.Equal("Ativo", user.StatusDisplay);
            Assert.False(user.HasRole("Admin"));
        }

        [Theory]
        [InlineData(0, 0, 0.0)]
        [InlineData(100, 80, 80.0)]
        [InlineData(3, 1, 33.333333)]
        public void DashboardMetrics_PercentualAtivos(int total, int ativos, double expected)
        {
            var metrics = new DashboardMetricsDto { TotalUsuarios = total, TotalUsuariosAtivos = ativos };

            Assert.Equal(expected, metrics.PercentualAtivos, 5);
        }

        [Fact]
        public void AuthResponse_SucessoIsInferredFromToken()
        {
            Assert.True(new AuthResponseDto { Token = "jwt" }.Sucesso);
            Assert.False(new AuthResponseDto().Sucesso);
            Assert.True(new AuthResponseDto { Sucesso = true }.Sucesso);
        }
    }

    public class BadgeFormTests
    {
        [Fact]
        public void Validate_RejectsShortName()
        {
            Assert.NotNull(new BadgeForm { Nome = "A" }.Validate());
            Assert.NotNull(new BadgeForm { Nome = "   " }.Validate());
        }

        [Fact]
        public void Validate_RejectsNegativeXp()
        {
            Assert.NotNull(new BadgeForm { Nome = "Pato", XpNecessario = -1 }.Validate());
        }

        [Fact]
        public void Validate_RejectsLongDescription()
        {
            Assert.NotNull(new BadgeForm { Nome = "Pato", Descricao = new string('x', 1001) }.Validate());
        }

        [Fact]
        public void Validate_AcceptsValidForm()
        {
            Assert.Null(new BadgeForm { Nome = "Pato Mestre", Descricao = "Ok", XpNecessario = 100 }.Validate());
        }

        [Fact]
        public void ToRequest_TrimsFields()
        {
            var request = new BadgeForm { Nome = "  Pato  ", Descricao = " d ", XpNecessario = 5, Ativa = false }.ToRequest();

            Assert.Equal("Pato", request.Nome);
            Assert.Equal("d", request.Descricao);
            Assert.Equal(5, request.XPNecessario);
            Assert.False(request.Ativa);
        }

        [Fact]
        public void From_CopiesDtoAndMarksEditMode()
        {
            var form = BadgeForm.From(new BadgeDto { ConquistaID = 4, Nome = "N", XPNecessario = 9, Ativa = false });

            Assert.True(form.IsEdicao);
            Assert.Equal("Editar medalha", form.Titulo);
            Assert.Equal(4, form.Id);
            Assert.False(form.Ativa);
            Assert.Equal("Nova medalha", new BadgeForm().Titulo);
        }
    }

    public class ChallengeFormTests
    {
        private static ChallengeForm Valid() => new() { Nome = "Maratona", RecompensaXp = 50 };

        [Fact]
        public void Validate_AcceptsMinimalForm()
        {
            Assert.Null(Valid().Validate());
        }

        [Fact]
        public void Validate_RejectsEndBeforeStart()
        {
            var form = Valid();
            form.InicioEm = new DateTime(2026, 12, 10);
            form.FimEm = new DateTime(2026, 12, 1);

            Assert.NotNull(form.Validate());
        }

        [Fact]
        public void Validate_RejectsEndEqualToStart()
        {
            var form = Valid();
            form.InicioEm = new DateTime(2026, 12, 10);
            form.FimEm = new DateTime(2026, 12, 10);

            Assert.NotNull(form.Validate());
        }

        [Fact]
        public void Validate_RequirementTypeNeedsGoal()
        {
            var form = Valid();
            form.TipoRequisito = "AULAS_CONCLUIDAS";
            form.MetaRequisito = 0;

            Assert.NotNull(form.Validate());

            form.MetaRequisito = 5;
            Assert.Null(form.Validate());
        }

        [Fact]
        public void Validate_RejectsUnknownRequirementType()
        {
            var form = Valid();
            form.TipoRequisito = "INVENTADO";

            Assert.NotNull(form.Validate());
        }

        [Fact]
        public void Validate_RejectsNegativeRewardAndInvalidIds()
        {
            var negative = Valid();
            negative.RecompensaXp = -5;
            Assert.NotNull(negative.Validate());

            var trilha = Valid();
            trilha.TrilhaId = 0;
            Assert.NotNull(trilha.Validate());

            var medalha = Valid();
            medalha.ConquistaId = -1;
            Assert.NotNull(medalha.Validate());
        }

        [Fact]
        public void ToRequest_NormalizesRequirementTypeToUpperCase()
        {
            var form = Valid();
            form.TipoRequisito = " xp_total ";
            form.MetaRequisito = 100;
            form.TrilhaId = 3;

            var request = form.ToRequest();

            Assert.Equal("XP_TOTAL", request.TipoRequisito);
            Assert.Equal(100, request.MetaRequisito);
            Assert.Equal(3, request.TrilhaID);
        }

        [Fact]
        public void From_WithoutRequirement_DefaultsToNenhum()
        {
            var form = ChallengeForm.From(new ChallengeDto { DesafioID = 8, Nome = "D", TipoRequisito = null });

            Assert.Equal("NENHUM", form.TipoRequisito);
            Assert.True(form.IsEdicao);
            Assert.Equal("Editar desafio", form.Titulo);
        }
    }
}
