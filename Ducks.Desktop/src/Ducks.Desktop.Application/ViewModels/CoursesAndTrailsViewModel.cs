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
    public partial class CoursesAndTrailsViewModel : ViewModelBase
    {
        private const string TodosStatus = "Todos";

        private readonly IContentApiService _contentService;
        private readonly IDialogService _dialogService;
        private readonly IContentManagementApiService? _managementService;
        private List<TrilhaDto> _todasTrilhas = new();

        [ObservableProperty] private ObservableCollection<TrilhaDto> _itens = new();
        [ObservableProperty] private TrilhaDto? _selectedItem;
        [ObservableProperty] private int _totalTrilhas;
        [ObservableProperty] private int _totalPendentes;
        [ObservableProperty] private int _totalAprovadas;
        [ObservableProperty] private string _filtroTermo = string.Empty;
        [ObservableProperty] private ObservableCollection<string> _opcoesStatus = new() { TodosStatus };
        [ObservableProperty] private string _filtroStatusSelecionado = TodosStatus;

        // Formulário de nova trilha
        [ObservableProperty] private string _novaTrilhaNome = string.Empty;
        [ObservableProperty] private string _novaTrilhaDescricao = string.Empty;
        [ObservableProperty] private int _novaTrilhaMateriaId;
        [ObservableProperty] private int? _trilhaEmEdicaoId;

        public CoursesAndTrailsViewModel(IContentApiService contentService, IDialogService dialogService,
            IContentManagementApiService? managementService = null)
        {
            _contentService = contentService;
            _dialogService = dialogService;
            _managementService = managementService;
        }

        public bool EditandoTrilha => TrilhaEmEdicaoId.HasValue;
        public string TituloFormulario => EditandoTrilha ? $"Editar trilha #{TrilhaEmEdicaoId}" : "Nova trilha";
        public string TextoBotaoSalvar => EditandoTrilha ? "Salvar alterações" : "Criar trilha";

        partial void OnTrilhaEmEdicaoIdChanged(int? value)
        {
            OnPropertyChanged(nameof(EditandoTrilha));
            OnPropertyChanged(nameof(TituloFormulario));
            OnPropertyChanged(nameof(TextoBotaoSalvar));
        }

        public override Task InitializeAsync() => CarregarDadosAsync();

        partial void OnFiltroTermoChanged(string value) => AplicarFiltros();
        partial void OnFiltroStatusSelecionadoChanged(string value) => AplicarFiltros();

        [RelayCommand]
        private Task AtualizarAsync() => CarregarDadosAsync();

        public async Task CarregarDadosAsync()
        {
            IReadOnlyList<TrilhaDto>? lista = null;
            var ok = await RunAsync(async () => lista = await _contentService.GetTrilhasAsync(), "Carregando catálogo educacional...");
            if (!ok || lista == null) return;

            _todasTrilhas = lista.ToList();
            TotalTrilhas = _todasTrilhas.Count;
            TotalPendentes = _todasTrilhas.Count(t => string.Equals(t.Status, "PENDENTE", StringComparison.OrdinalIgnoreCase));
            TotalAprovadas = _todasTrilhas.Count(t => string.Equals(t.Status, "APROVADO", StringComparison.OrdinalIgnoreCase)
                                                    || string.Equals(t.Status, "APROVADA", StringComparison.OrdinalIgnoreCase));

            var selecionado = FiltroStatusSelecionado;
            var status = _todasTrilhas.Select(t => t.Status).Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s);
            OpcoesStatus = new ObservableCollection<string>(new[] { TodosStatus }.Concat(status));
            FiltroStatusSelecionado = OpcoesStatus.Contains(selecionado) ? selecionado : TodosStatus;

            AplicarFiltros();
            SetStatus(_todasTrilhas.Count == 0 ? "Nenhuma trilha cadastrada." : string.Empty);
        }

        private void AplicarFiltros()
        {
            IEnumerable<TrilhaDto> resultado = _todasTrilhas;

            if (!string.IsNullOrWhiteSpace(FiltroTermo))
            {
                var termo = FiltroTermo.Trim();
                resultado = resultado.Where(t =>
                    t.NomeTrilha.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                    t.Descricao.Contains(termo, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(FiltroStatusSelecionado) && FiltroStatusSelecionado != TodosStatus)
                resultado = resultado.Where(t => string.Equals(t.Status, FiltroStatusSelecionado, StringComparison.OrdinalIgnoreCase));

            Itens = new ObservableCollection<TrilhaDto>(resultado);
        }

        public string? ValidarNovaTrilha()
        {
            var nome = NovaTrilhaNome?.Trim() ?? string.Empty;
            if (nome.Length < 3 || nome.Length > 150) return "O nome da trilha deve ter entre 3 e 150 caracteres.";
            if ((NovaTrilhaDescricao?.Length ?? 0) > 2000) return "A descrição deve ter no máximo 2000 caracteres.";
            if (NovaTrilhaMateriaId < 1) return "Informe o ID da matéria (maior que zero).";
            return null;
        }

        [RelayCommand]
        private void EditarTrilha()
        {
            var alvo = SelectedItem;
            if (alvo == null) { SetStatus("Selecione uma trilha na tabela para editar.", true); return; }

            TrilhaEmEdicaoId = alvo.TrilhaID;
            NovaTrilhaNome = alvo.NomeTrilha;
            NovaTrilhaDescricao = alvo.Descricao ?? string.Empty;
            NovaTrilhaMateriaId = alvo.MateriaID;
            SetStatus($"Editando a trilha '{alvo.NomeTrilha}'. Salve para aplicar as alterações.");
        }

        [RelayCommand]
        private void CancelarEdicao()
        {
            LimparFormulario();
            SetStatus(string.Empty);
        }

        private void LimparFormulario()
        {
            TrilhaEmEdicaoId = null;
            NovaTrilhaNome = string.Empty;
            NovaTrilhaDescricao = string.Empty;
            NovaTrilhaMateriaId = 0;
        }

        [RelayCommand]
        private async Task CriarTrilhaAsync()
        {
            var erro = ValidarNovaTrilha();
            if (erro != null) { SetStatus(erro, true); return; }

            var dto = new CreateTrilhaRequestDto
            {
                NomeTrilha = NovaTrilhaNome.Trim(),
                Descricao = (NovaTrilhaDescricao ?? string.Empty).Trim(),
                MateriaID = NovaTrilhaMateriaId
            };

            var editandoId = TrilhaEmEdicaoId;
            if (editandoId.HasValue)
            {
                if (_managementService == null) { SetStatus("Edição de trilha indisponível nesta configuração.", true); return; }

                var atualizou = await RunAsync(() => _managementService.UpdateTrilhaAsync(editandoId.Value, dto));
                if (!atualizou) return;

                LimparFormulario();
                await CarregarDadosAsync();
                if (!HasError) SetStatus($"Trilha '{dto.NomeTrilha}' atualizada (volta a RASCUNHO até nova revisão).");
                return;
            }

            var sucesso = await RunAsync(() => _contentService.CreateTrilhaAsync(dto));
            if (!sucesso) return;

            LimparFormulario();

            await CarregarDadosAsync();
            if (!HasError) SetStatus($"Trilha '{dto.NomeTrilha}' criada com status PENDENTE.");
        }

        [RelayCommand]
        private async Task ExcluirTrilhaAsync()
        {
            var alvo = SelectedItem;
            if (alvo == null) { SetStatus("Selecione uma trilha na tabela para excluir.", true); return; }

            if (!_dialogService.ShowConfirmation("Confirmar exclusão",
                    $"Deseja excluir a trilha '{alvo.NomeTrilha}'?\nEsta ação não pode ser desfeita.")) return;

            var sucesso = await RunAsync(() => _contentService.DeleteTrilhaAsync(alvo.TrilhaID));
            if (!sucesso) return;

            await CarregarDadosAsync();
            if (!HasError) SetStatus($"Trilha '{alvo.NomeTrilha}' excluída.");
        }

        [RelayCommand]
        private async Task EnviarRevisaoAsync()
        {
            var alvo = SelectedItem;
            if (alvo == null) { SetStatus("Selecione uma trilha na tabela para enviar à revisão.", true); return; }

            var sucesso = await RunAsync(() => _contentService.SendTrilhaToReviewAsync(alvo.TrilhaID));
            if (!sucesso) return;

            await CarregarDadosAsync();
            if (!HasError) SetStatus($"Trilha '{alvo.NomeTrilha}' enviada para revisão.");
        }
    }
}
