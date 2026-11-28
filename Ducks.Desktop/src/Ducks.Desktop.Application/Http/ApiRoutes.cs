using System;

namespace Ducks.Desktop.Application.Http
{
    /// <summary>
    /// Todas as rotas da Ducks.Api usadas pelo Desktop, em um único lugar.
    /// Fonte: controllers da API (AdminUsers, AdminRoles, AdminMetrics, AdminAudit, AdminSettings,
    /// AdminGamification, AdminContent, AdminModulosAulas, AdminAvaliacoes, AdminAi, Moderacao, Auth).
    /// </summary>
    public static class ApiRoutes
    {
        // Auth
        public const string Login = "api/auth/login";
        public const string Refresh = "api/auth/refresh";

        // Usuários e papéis
        public const string AdminUsers = "api/admin/users";
        public static string AdminUsersPage(string? search, int page, int pageSize)
        {
            var url = $"{AdminUsers}?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search.Trim())}";
            return url;
        }
        public static string AdminUserBlock(int usuarioId, bool block) =>
            $"{AdminUsers}/{usuarioId}/block?block={(block ? "true" : "false")}";
        public const string AdminRoles = "api/admin/roles";
        public const string AdminRolesAssign = "api/admin/roles/assign";

        // Métricas, auditoria, configurações, IA
        public const string AdminMetricsSummary = "api/admin/metrics/summary";
        public const string AdminAuditLogs = "api/admin/audit/logs";
        public const string AdminSettings = "api/admin/settings";
        public const string AdminAiAutomate = "api/admin/ai/automate";

        // Gamificação
        public const string GamificationBadges = "api/admin/gamification/badges";
        public static string GamificationBadge(int id) => $"{GamificationBadges}/{id}";
        public static string GamificationUserBadges(int usuarioId) => $"api/admin/gamification/users/{usuarioId}/badges";
        public const string GamificationChallenges = "api/admin/gamification/challenges";
        public static string GamificationChallenge(int id) => $"{GamificationChallenges}/{id}";

        // Conteúdo (trilhas)
        public const string ContentTrilhas = "api/admin/content/trilhas";
        public static string ContentTrilha(int id) => $"{ContentTrilhas}/{id}";
        public static string ContentTrilhaEnviarRevisao(int id) => $"{ContentTrilhas}/{id}/enviar-revisao";

        // Módulos e aulas (AdminModulosAulasController)
        public static string ContentModulos(int trilhaId, int page = 1, int pageSize = 100) =>
            $"{ContentTrilhas}/{trilhaId}/modulos?page={page}&pageSize={pageSize}";
        public static string ContentModulosCriar(int trilhaId) => $"{ContentTrilhas}/{trilhaId}/modulos";
        public static string ContentModulosOrdem(int trilhaId) => $"{ContentTrilhas}/{trilhaId}/modulos/ordem";
        public static string ContentModulo(int moduloId) => $"api/admin/content/modulos/{moduloId}";
        public static string ContentModuloEnviarRevisao(int moduloId) => $"{ContentModulo(moduloId)}/enviar-revisao";
        public static string ContentAulas(int moduloId, int page = 1, int pageSize = 100) =>
            $"{ContentModulo(moduloId)}/aulas?page={page}&pageSize={pageSize}";
        public static string ContentAulasCriar(int moduloId) => $"{ContentModulo(moduloId)}/aulas";
        public static string ContentAulasOrdem(int moduloId) => $"{ContentModulo(moduloId)}/aulas/ordem";
        public static string ContentAula(int aulaId) => $"api/admin/content/aulas/{aulaId}";
        public static string ContentAulaEnviarRevisao(int aulaId) => $"{ContentAula(aulaId)}/enviar-revisao";

        // Banco de questões e simulados (AdminAvaliacoesController)
        public const string ContentQuestoes = "api/admin/content/questoes";
        public static string ContentQuestoesPage(string? busca, string? status, string? dificuldade, int page, int pageSize)
        {
            var url = $"{ContentQuestoes}?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(busca)) url += $"&busca={Uri.EscapeDataString(busca.Trim())}";
            if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";
            if (!string.IsNullOrWhiteSpace(dificuldade)) url += $"&dificuldade={Uri.EscapeDataString(dificuldade)}";
            return url;
        }
        public static string ContentQuestao(int id) => $"{ContentQuestoes}/{id}";
        public static string ContentQuestaoEnviarRevisao(int id) => $"{ContentQuestao(id)}/enviar-revisao";

        public const string ContentSimulados = "api/admin/content/simulados";
        public static string ContentSimuladosPage(string? busca, string? status, int page, int pageSize)
        {
            var url = $"{ContentSimulados}?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(busca)) url += $"&busca={Uri.EscapeDataString(busca.Trim())}";
            if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";
            return url;
        }
        public static string ContentSimulado(int id) => $"{ContentSimulados}/{id}";
        public static string ContentSimuladoQuestoes(int id) => $"{ContentSimulado(id)}/questoes";
        public static string ContentSimuladoSortear(int id) => $"{ContentSimulado(id)}/questoes/sortear";
        public static string ContentSimuladoEnviarRevisao(int id) => $"{ContentSimulado(id)}/enviar-revisao";

        // XP (EducacionalController, somente Admin)
        public const string XpAdicionar = "api/educacional/xp/adicionar";

        // Moderação
        public const string ModeracaoPendentes = "api/moderacao/pendentes";
        public const string ModeracaoHistorico = "api/moderacao/historico";
        public static string ModeracaoAprovar(string tipo, int id) => $"api/moderacao/{Uri.EscapeDataString(tipo)}/{id}/aprovar";
        public static string ModeracaoRejeitar(string tipo, int id) => $"api/moderacao/{Uri.EscapeDataString(tipo)}/{id}/rejeitar";
        public static string ModeracaoRemover(string tipo, int id) => $"api/moderacao/{Uri.EscapeDataString(tipo)}/{id}/remover";

        // SignalR (opcional, melhor esforço)
        public const string ModerationHub = "hubs/moderation";
    }
}
