using Ducks.Desktop.Application.Http;

namespace Ducks.Desktop.Tests
{
    /// <summary>Garante que o Desktop aponta para as rotas reais dos controllers da Ducks.Api.</summary>
    public class ApiRoutesTests
    {
        [Fact]
        public void StaticRoutes_MatchApiControllers()
        {
            Assert.Equal("api/auth/login", ApiRoutes.Login);
            Assert.Equal("api/auth/refresh", ApiRoutes.Refresh);
            Assert.Equal("api/admin/users", ApiRoutes.AdminUsers);
            Assert.Equal("api/admin/roles", ApiRoutes.AdminRoles);
            Assert.Equal("api/admin/roles/assign", ApiRoutes.AdminRolesAssign);
            Assert.Equal("api/admin/metrics/summary", ApiRoutes.AdminMetricsSummary);
            Assert.Equal("api/admin/audit/logs", ApiRoutes.AdminAuditLogs);
            Assert.Equal("api/admin/settings", ApiRoutes.AdminSettings);
            Assert.Equal("api/admin/ai/automate", ApiRoutes.AdminAiAutomate);
            Assert.Equal("api/admin/gamification/badges", ApiRoutes.GamificationBadges);
            Assert.Equal("api/admin/gamification/challenges", ApiRoutes.GamificationChallenges);
            Assert.Equal("api/admin/content/trilhas", ApiRoutes.ContentTrilhas);
            Assert.Equal("api/moderacao/pendentes", ApiRoutes.ModeracaoPendentes);
            Assert.Equal("api/moderacao/historico", ApiRoutes.ModeracaoHistorico);
        }

        [Fact]
        public void AdminUsersPage_WithoutSearch_OmitsSearchParameter()
        {
            Assert.Equal("api/admin/users?page=1&pageSize=25", ApiRoutes.AdminUsersPage(null, 1, 25));
            Assert.Equal("api/admin/users?page=1&pageSize=25", ApiRoutes.AdminUsersPage("   ", 1, 25));
        }

        [Fact]
        public void AdminUsersPage_EscapesSearchTerm()
        {
            var url = ApiRoutes.AdminUsersPage("  João & Ana ", 2, 10);

            Assert.Equal("api/admin/users?page=2&pageSize=10&search=Jo%C3%A3o%20%26%20Ana", url);
        }

        [Theory]
        [InlineData(true, "api/admin/users/5/block?block=true")]
        [InlineData(false, "api/admin/users/5/block?block=false")]
        public void AdminUserBlock_BuildsQueryString(bool block, string expected)
        {
            Assert.Equal(expected, ApiRoutes.AdminUserBlock(5, block));
        }

        [Fact]
        public void ParameterizedRoutes_BuildExpectedPaths()
        {
            Assert.Equal("api/admin/gamification/badges/3", ApiRoutes.GamificationBadge(3));
            Assert.Equal("api/admin/gamification/challenges/4", ApiRoutes.GamificationChallenge(4));
            Assert.Equal("api/admin/gamification/users/9/badges", ApiRoutes.GamificationUserBadges(9));
            Assert.Equal("api/admin/content/trilhas/7", ApiRoutes.ContentTrilha(7));
            Assert.Equal("api/admin/content/trilhas/7/enviar-revisao", ApiRoutes.ContentTrilhaEnviarRevisao(7));
            Assert.Equal("api/moderacao/TRILHA/12/aprovar", ApiRoutes.ModeracaoAprovar("TRILHA", 12));
            Assert.Equal("api/moderacao/AULA/12/rejeitar", ApiRoutes.ModeracaoRejeitar("AULA", 12));
            Assert.Equal("api/moderacao/QUESTAO/12/remover", ApiRoutes.ModeracaoRemover("QUESTAO", 12));
        }

        [Fact]
        public void ModeracaoRoutes_EscapeTypeSegment()
        {
            Assert.Equal("api/moderacao/a%2Fb/1/aprovar", ApiRoutes.ModeracaoAprovar("a/b", 1));
        }
    }
}
