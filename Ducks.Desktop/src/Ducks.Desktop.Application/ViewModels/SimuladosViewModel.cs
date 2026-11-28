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
    /// <summary>Simulados: CRUD, seleção manual de questões, sorteio por filtros e envio para revisão.</summary>
    public partial class SimuladosViewModel : ViewModelBase
    {
        public const string Todos = "Todos";
        public const int TamanhoPagina = 25;

        private readonly IAssessmentApiService _service;
        private readonly IDialogService _dialog;

        public IReadOnlyList<string> OpcoesStatus { get; } = new[] { Todos, "RASCUNHO", "PENDENTE", "APROVADO", "REJEITADO", "REMOVIDO" };
        public IReadOnlyList<string> OpcoesDificuldadeSorteio { get; } = new[] { Todos, "FACIL", "MEDIO", "DIFICIL" };

        // Listagem
        [ObservableProperty] private ObservableCollection<SimuladoDto> _itens = new();
        [ObservableProperty] private SimuladoDto? _selectedItem;
        [ObservableProperty] private string _filtroBusca = string.Empty;
        [ObservableProperty] private string _filtroStatus = Todos;
        [ObservableProperty] private int _pagina = 1;
        [ObservableProperty] private int _totalPaginas = 1;
        [ObservableProperty] private int _totalItens;

        // Formulário
        [ObservableProperty] private int? _emEdicaoId;
        [ObservableProperty] private string _titulo = string.Empty;
        [ObservableProperty] private string _descricao = string.Empty;
        [ObservableProperty] private string _trilhaIdTexto = string.Empty;
        [ObservableProperty] private string _moduloIdTexto = string.Empty;
        [ObservableProperty] private string _tempoLimiteTexto = string.Empty;
        [ObservableProperty] private string _maxTentativasTexto = string.Empty;
        [ObservableProperty] private string _notaMinimaTexto = "60";
        [ObservableProperty] private string _recompensaXpTexto = "50";
        [ObservableProperty] private string _numeroQuestoesTexto = "10";

        // Questões do simulado selecionado
        [ObservableProperty] private ObservableCollection<QuestaoDoSimuladoDto> _questoes = new();
        [ObservableProperty] private string _questaoIdsTexto = string.Empty;

        // Sorteio
        [ObservableProperty] private string _sorteioQuantidadeTexto = string.Empty;
        [ObservableProperty] private string _sorteioDificuldade = Todos;
        [ObservableProperty] private string _sorteioTag = string.Empty;
        [ObservableProperty] private string _sorteioAssunto = string.Empty;
        [ObservableProperty] private bool _sorteioSubstituir = true;

        public SimuladosViewModel(IAssessmentApiService service, IDialogService dialog)
        {
            _service = service;
            _dialog = dialog;
        }

        public bool Editando => EmEdicaoId.HasValue;
        public string TituloFormulario => Editando ? $"Editar simulado #{EmEdicaoId}" : "Novo simulado";
        partial void OnEmEdicaoIdChanged(int? value)
        {
            OnPropertyChanged(nameof(Editando));
            OnPropertyChanged(nameof(TituloFormulario));
        }

        partial void OnSelectedItemChanged(SimuladoDto? value) => AtualizarQuestoes(value);

        public override Task InitializeAsync() => CarregarAsync();

        // ------------------------------------------------------------------ Listagem

        [RelayCommand]
        private Task BuscarAsync()
        {
            Pagina = 1;
            return CarregarAsync();
        }

        [RelayCommand]
        private async Task PaginaAnteriorAsync()
        {
            if (Pagina <= 1) return;
            Pagina--;
            await CarregarAsync();
        }

        [RelayCommand]
        private async Task PaginaSeguinteAsync()
        {
            if (Pagina >= TotalPaginas) return;
            Pagina++;
            await CarregarAsync();
        }

        public async Task CarregarAsync()
        {
            PagedResult<SimuladoDto>? resultado = null;
            var ok = await RunAsync(async () => resultado = await _service.GetSimuladosAsync(
                string.IsNullOrWhiteSpace(FiltroBusca) ? null : FiltroBusca.Trim(),
                FiltroStatus == Todos ? null : FiltroStatus,
                Pagina, TamanhoPagina), "Carregando simulados...");
            if (!ok || resultado == null) return;

            Itens = new ObservableCollection<SimuladoDto>(resultado.Items);
            TotalItens = resultado.Total;
            TotalPaginas = resultado.TotalPages;
            SetStatus(Itens.Count == 0 ? "Nenhum simulado encontrado para os filtros informados." : string.Empty);
        }

        private void AtualizarQuestoes(SimuladoDto? simulado)
        {
            var lista = (simulado?.Questoes ?? new List<QuestaoDoSimuladoDto>()).OrderBy(q => q.Ordem).ToList();
            Questoes = new ObservableCollection<QuestaoDoSimuladoDto>(lista);
            QuestaoIdsTexto = string.Join(", ", lista.Select(q => q.QuestaoID));
        }

        // ------------------------------------------------------------------ Formulário

        [RelayCommand]
        private void NovoSimulado()
        {
            EmEdicaoId = null;
            Titulo = string.Empty;
            Descricao = string.Empty;
            TrilhaIdTexto = string.Empty;
            ModuloIdTexto = string.Empty;
            TempoLimiteTexto = string.Empty;
            MaxTentativasTexto = string.Empty;
            NotaMinimaTexto = "60";
            RecompensaXpTexto = "50";
            NumeroQuestoesTexto = "10";
        }

        [RelayCommand]
        private void EditarSimulado()
        {
            var s = SelectedItem;
            if (s == null) { SetStatus("Selecione um simulado na tabela para editar.", true); return; }

            EmEdicaoId = s.ProvaID;
            Titulo = s.Titulo;
            Descricao = s.Descricao ?? string.Empty;
            TrilhaIdTexto = s.TrilhaID?.ToString() ?? string.Empty;
            ModuloIdTexto = s.ModuloID?.ToString() ?? string.Empty;
            TempoLimiteTexto = s.TempoLimiteMinutos?.ToString() ?? string.Empty;
            MaxTentativasTexto = s.MaxTentativas?.ToString() ?? string.Empty;
            NotaMinimaTexto = s.NotaMinimaAprovacao.ToString();
            RecompensaXpTexto = s.RecompensaXP.ToString();
            NumeroQuestoesTexto = s.NumeroQuestoes.ToString();
            SetStatus($"Editando o simulado '{s.Titulo}'. Salve para aplicar as alterações.");
        }

        public string? Validar(out SimuladoRequestDto? dto)
        {
            dto = null;
            var titulo = Titulo?.Trim() ?? string.Empty;
            if (titulo.Length < 3 || titulo.Length > 150) return "O título deve ter entre 3 e 150 caracteres.";
            if ((Descricao?.Length ?? 0) > 2000) return "A descrição deve ter no máximo 2000 caracteres.";

            if (!TryOptional(TrilhaIdTexto, 1, int.MaxValue, out var trilhaId)) return "O ID da trilha deve ser um número maior que zero (ou fique em branco).";
            if (!TryOptional(ModuloIdTexto, 1, int.MaxValue, out var moduloId)) return "O ID do módulo deve ser um número maior que zero (ou fique em branco).";
            if (!TryOptional(TempoLimiteTexto, 1, 600, out var tempo)) return "O tempo limite deve estar entre 1 e 600 minutos (ou fique em branco para sem limite).";
            if (!TryOptional(MaxTentativasTexto, 1, 100, out var tentativas)) return "O máximo de tentativas deve estar entre 1 e 100 (ou fique em branco para ilimitado).";

            if (!int.TryParse(NotaMinimaTexto?.Trim(), out var nota) || nota < 0 || nota > 100) return "A nota mínima deve estar entre 0 e 100.";
            if (!int.TryParse(RecompensaXpTexto?.Trim(), out var xp) || xp < 0 || xp > 10000) return "O XP de recompensa deve estar entre 0 e 10000.";
            if (!int.TryParse(NumeroQuestoesTexto?.Trim(), out var numero) || numero < 1 || numero > 200) return "O número de questões deve estar entre 1 e 200.";

            dto = new SimuladoRequestDto
            {
                Titulo = titulo,
                Descricao = string.IsNullOrWhiteSpace(Descricao) ? null : Descricao.Trim(),
                TrilhaID = trilhaId,
                ModuloID = moduloId,
                TempoLimiteMinutos = tempo,
                MaxTentativas = tentativas,
                NotaMinimaAprovacao = nota,
                RecompensaXP = xp,
                NumeroQuestoes = numero
            };
            return null;
        }

        private static bool TryOptional(string? texto, int min, int max, out int? valor)
        {
            valor = null;
            if (string.IsNullOrWhiteSpace(texto)) return true;
            if (int.TryParse(texto.Trim(), out var n) && n >= min && n <= max) { valor = n; return true; }
            return false;
        }

        [RelayCommand]
        private async Task SalvarAsync()
        {
            var erro = Validar(out var dto);
            if (erro != null || dto == null) { SetStatus(erro ?? "Dados inválidos.", true); return; }

            var editando = EmEdicaoId;
            var ok = await RunAsync(() => editando.HasValue
                ? _service.UpdateSimuladoAsync(editando.Value, dto)
                : _service.CreateSimuladoAsync(dto));
            if (!ok) return;

            NovoSimulado();
            await CarregarAsync();
            if (!HasError) SetStatus(editando.HasValue ? $"Simulado '{dto.Titulo}' atualizado." : $"Simulado '{dto.Titulo}' criado como RASCUNHO.");
        }

        [RelayCommand]
        private async Task ExcluirAsync()
        {
            var alvo = SelectedItem;
            if (alvo == null) { SetStatus("Selecione um simulado na tabela para excluir.", true); return; }
            if (!_dialog.ShowConfirmation("Confirmar exclusão", $"Deseja excluir o simulado '{alvo.Titulo}'?\nEsta ação não pode ser desfeita.")) return;

            var ok = await RunAsync(() => _service.DeleteSimuladoAsync(alvo.ProvaID));
            if (!ok) return;

            if (EmEdicaoId == alvo.ProvaID) NovoSimulado();
            await CarregarAsync();
            if (!HasError) SetStatus($"Simulado '{alvo.Titulo}' excluído.");
        }

        [RelayCommand]
        private async Task EnviarRevisaoAsync()
        {
            var alvo = SelectedItem;
            if (alvo == null) { SetStatus("Selecione um simulado na tabela para enviar à revisão.", true); return; }

            var ok = await RunAsync(() => _service.SendSimuladoToReviewAsync(alvo.ProvaID));
            if (!ok) return;

            await CarregarAsync();
            if (!HasError) SetStatus($"Simulado '{alvo.Titulo}' enviado para revisão.");
        }

        // ------------------------------------------------------------------ Questões do simulado

        /// <summary>Converte "1, 2, 3" na lista de IDs (sem repetidos, na ordem informada). Retorna null se inválido.</summary>
        public static List<int>? ParseIds(string? texto)
        {
            var partes = (texto ?? string.Empty).Split(new[] { ',', ';', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var ids = new List<int>();
            foreach (var parte in partes)
            {
                if (!int.TryParse(parte, out var id) || id < 1) return null;
                if (!ids.Contains(id)) ids.Add(id);
            }
            return ids;
        }

        [RelayCommand]
        private async Task DefinirQuestoesAsync()
        {
            var alvo = SelectedItem;
            if (alvo == null) { SetStatus("Selecione um simulado na tabela para definir as questões.", true); return; }

            var ids = ParseIds(QuestaoIdsTexto);
            if (ids == null) { SetStatus("Informe os IDs das questões separados por vírgula (apenas números maiores que zero).", true); return; }
            if (ids.Count < 1 || ids.Count > 200) { SetStatus("Informe de 1 a 200 questões.", true); return; }

            SimuladoDto? atualizado = null;
            var ok = await RunAsync(async () => atualizado = await _service.SetQuestoesAsync(alvo.ProvaID, ids));
            if (!ok) return;

            await RecarregarSelecionadoAsync(alvo.ProvaID, atualizado);
            if (!HasError) SetStatus($"{ids.Count} questão(ões) definida(s) para '{alvo.Titulo}'.");
        }

        public string? ValidarSorteio(out SortearQuestoesRequestDto? dto)
        {
            dto = null;
            int? quantidade = null;
            if (!string.IsNullOrWhiteSpace(SorteioQuantidadeTexto))
            {
                if (!int.TryParse(SorteioQuantidadeTexto.Trim(), out var q) || q < 1 || q > 200)
                    return "A quantidade do sorteio deve estar entre 1 e 200 (ou fique em branco para usar o número de questões do simulado).";
                quantidade = q;
            }

            dto = new SortearQuestoesRequestDto
            {
                Quantidade = quantidade,
                Dificuldade = SorteioDificuldade == Todos || string.IsNullOrWhiteSpace(SorteioDificuldade) ? null : SorteioDificuldade,
                Tag = string.IsNullOrWhiteSpace(SorteioTag) ? null : SorteioTag.Trim(),
                Assunto = string.IsNullOrWhiteSpace(SorteioAssunto) ? null : SorteioAssunto.Trim(),
                SubstituirAtuais = SorteioSubstituir
            };
            return null;
        }

        [RelayCommand]
        private async Task SortearAsync()
        {
            var alvo = SelectedItem;
            if (alvo == null) { SetStatus("Selecione um simulado na tabela para sortear as questões.", true); return; }
            var erro = ValidarSorteio(out var dto);
            if (erro != null || dto == null) { SetStatus(erro ?? "Dados inválidos.", true); return; }

            if (dto.SubstituirAtuais && alvo.Questoes.Count > 0 &&
                !_dialog.ShowConfirmation("Substituir questões", "O sorteio vai substituir as questões atuais do simulado. Deseja continuar?")) return;

            SimuladoDto? atualizado = null;
            var ok = await RunAsync(async () => atualizado = await _service.SortearQuestoesAsync(alvo.ProvaID, dto));
            if (!ok) return;

            await RecarregarSelecionadoAsync(alvo.ProvaID, atualizado);
            if (!HasError) SetStatus($"Sorteio concluído para '{alvo.Titulo}'.");
        }

        /// <summary>Recarrega a lista mantendo o simulado selecionado (usa o retorno da API quando disponível).</summary>
        private async Task RecarregarSelecionadoAsync(int provaId, SimuladoDto? atualizado)
        {
            await CarregarAsync();
            var item = Itens.FirstOrDefault(i => i.ProvaID == provaId);
            if (item != null && atualizado != null && item.Questoes.Count == 0)
                item.Questoes = atualizado.Questoes;
            SelectedItem = item;
            if (item == null && atualizado != null) AtualizarQuestoes(atualizado);
        }
    }
}
