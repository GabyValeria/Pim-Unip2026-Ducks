using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels
{
    public class MetricBarItem
    {
        public string Rotulo { get; set; } = string.Empty;
        public int Valor { get; set; }
        public string ValorTexto { get; set; } = string.Empty;
        public double Altura { get; set; }
    }

    public partial class MetricsDashboardViewModel : ViewModelBase
    {
        public const double AlturaMaximaBarra = 260;
        private const double AlturaMinimaBarra = 8;

        private readonly IMetricsApiService _metricsService;
        private readonly IRoleApiService _roleService;

        [ObservableProperty] private string _totalUsuariosTexto = "—";
        [ObservableProperty] private string _usuariosAtivosPill = string.Empty;
        [ObservableProperty] private string _totalAulasTexto = "—";
        [ObservableProperty] private string _trilhasPill = string.Empty;
        [ObservableProperty] private string _pendenciasTexto = "—";
        [ObservableProperty] private string _pendenciasPill = string.Empty;
        [ObservableProperty] private string _xpDistribuidoTexto = "—";
        [ObservableProperty] private ObservableCollection<MetricBarItem> _barras = new();
        [ObservableProperty] private ObservableCollection<RoleDto> _papeis = new();

        public MetricsDashboardViewModel(IMetricsApiService metricsService, IRoleApiService roleService)
        {
            _metricsService = metricsService;
            _roleService = roleService;
        }

        public override Task InitializeAsync() => CarregarMetricasAsync();

        [RelayCommand]
        private Task AtualizarAsync() => CarregarMetricasAsync();

        public async Task CarregarMetricasAsync()
        {
            DashboardMetricsDto? metricas = null;
            var ok = await RunAsync(async () =>
            {
                metricas = await _metricsService.GetDashboardMetricsAsync();
            }, "Atualizando métricas da plataforma...");

            if (!ok || metricas == null) return;

            Aplicar(metricas);
            await CarregarPapeisAsync();
        }

        private async Task CarregarPapeisAsync()
        {
            try
            {
                var papeis = await _roleService.GetRolesAsync();
                Papeis = new ObservableCollection<RoleDto>(papeis);
            }
            catch
            {
                // Cartão de papéis é complementar: a falha não deve esconder as métricas já carregadas.
                Papeis = new ObservableCollection<RoleDto>();
            }
        }

        private void Aplicar(DashboardMetricsDto m)
        {
            TotalUsuariosTexto = FormatarCompacto(m.TotalUsuarios);
            UsuariosAtivosPill = $"{m.PercentualAtivos.ToString("0", CultureInfo.InvariantCulture)}% ativos";
            TotalAulasTexto = FormatarCompacto(m.TotalAulasCadastradas);
            TrilhasPill = $"{m.TotalTrilhas} trilha(s)";
            PendenciasTexto = FormatarCompacto(m.ItensPendentesModeracao);
            PendenciasPill = m.ItensPendentesModeracao > 0 ? "Aguardando revisão" : "Fila em dia";
            XpDistribuidoTexto = $"{FormatarCompacto(m.TotalXpDistribuido)} XP distribuídos";

            var itens = new (string Rotulo, int Valor)[]
            {
                ("Usuários", m.TotalUsuarios),
                ("Ativos", m.TotalUsuariosAtivos),
                ("Trilhas", m.TotalTrilhas),
                ("Aulas", m.TotalAulasCadastradas),
                ("Pendentes", m.ItensPendentesModeracao)
            };

            var max = Math.Max(1, itens.Max(i => i.Valor));
            Barras = new ObservableCollection<MetricBarItem>(itens.Select(i => new MetricBarItem
            {
                Rotulo = i.Rotulo,
                Valor = i.Valor,
                ValorTexto = FormatarCompacto(i.Valor),
                Altura = Math.Max(AlturaMinimaBarra, i.Valor * AlturaMaximaBarra / max)
            }));
        }

        /// <summary>12400 → "12.4k"; 1500000 → "1.5M".</summary>
        public static string FormatarCompacto(long valor)
        {
            var c = CultureInfo.InvariantCulture;
            if (valor >= 1_000_000) return (valor / 1_000_000d).ToString("0.#", c) + "M";
            if (valor >= 1_000) return (valor / 1_000d).ToString("0.#", c) + "k";
            return valor.ToString(c);
        }
    }
}
