using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels
{
    public partial class AuditLogsViewModel : ViewModelBase
    {
        private readonly IAuditApiService _auditService;
        private List<AuditLogDto> _todosLogs = new();

        [ObservableProperty]
        private ObservableCollection<AuditLogDto> _logs = new();

        [ObservableProperty]
        private AuditLogDto? _selectedLog;

        [ObservableProperty]
        private string _filtroTermo = string.Empty;

        public AuditLogsViewModel(IAuditApiService auditService)
        {
            _auditService = auditService;
        }

        public override Task InitializeAsync() => CarregarLogsAsync();

        partial void OnFiltroTermoChanged(string value) => AplicarFiltro();

        [RelayCommand]
        private Task AtualizarAsync() => CarregarLogsAsync();

        public async Task CarregarLogsAsync()
        {
            IReadOnlyList<AuditLogDto>? logs = null;
            var ok = await RunAsync(async () => logs = await _auditService.GetLogsAsync(), "Carregando logs de auditoria...");
            if (!ok || logs == null) return;

            _todosLogs = logs.ToList();
            AplicarFiltro();
            SetStatus(_todosLogs.Count == 0 ? "Nenhum registro de auditoria encontrado." : $"{_todosLogs.Count} registro(s) mais recentes.");
        }

        private void AplicarFiltro()
        {
            IEnumerable<AuditLogDto> resultado = _todosLogs;
            if (!string.IsNullOrWhiteSpace(FiltroTermo))
            {
                var termo = FiltroTermo.Trim();
                resultado = resultado.Where(l =>
                    l.Usuario.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    l.Acao.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    l.Detalhes.Contains(termo, StringComparison.OrdinalIgnoreCase));
            }
            Logs = new ObservableCollection<AuditLogDto>(resultado);
        }
    }
}
