using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.ViewModels.Forms;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels
{
    /// <summary>Banco de questões: listagem paginada com filtros, criação/edição com alternativas, exclusão e envio para revisão.</summary>
    public partial class QuestionBankViewModel : ViewModelBase
    {
        public const string Todos = "Todos";
        public const int TamanhoPagina = 25;
        public const int MaxAlternativas = 6;

        private readonly IAssessmentApiService _service;
        private readonly IDialogService _dialog;

        public IReadOnlyList<string> OpcoesStatus { get; } = new[] { Todos, "RASCUNHO", "PENDENTE", "APROVADO", "REJEITADO", "REMOVIDO" };
        public IReadOnlyList<string> OpcoesDificuldadeFiltro { get; } = new[] { Todos, "FACIL", "MEDIO", "DIFICIL" };
        public IReadOnlyList<string> OpcoesDificuldade { get; } = new[] { "FACIL", "MEDIO", "DIFICIL" };
        public IReadOnlyList<string> OpcoesTipo { get; } = new[] { "MULTIPLA_ESCOLHA", "VERDADEIRO_FALSO" };

        // Listagem
        [ObservableProperty] private ObservableCollection<QuestaoDto> _itens = new();
        [ObservableProperty] private QuestaoDto? _selectedItem;
        [ObservableProperty] private string _filtroBusca = string.Empty;
        [ObservableProperty] private string _filtroStatus = Todos;
        [ObservableProperty] private string _filtroDificuldade = Todos;
        [ObservableProperty] private int _pagina = 1;
        [ObservableProperty] private int _totalPaginas = 1;
        [ObservableProperty] private int _totalItens;

        // Formulário
        [ObservableProperty] private int? _emEdicaoId;
        [ObservableProperty] private string _enunciado = string.Empty;
        [ObservableProperty] private string _tipo = "MULTIPLA_ESCOLHA";
        [ObservableProperty] private string _dificuldade = "MEDIO";
        [ObservableProperty] private string _pesoTexto = "1";
        [ObservableProperty] private string _explicacaoGeral = string.Empty;
        [ObservableProperty] private string _assunto = string.Empty;
        [ObservableProperty] private string _tagsTexto = string.Empty;
        [ObservableProperty] private string _trilhaIdTexto = string.Empty;
        [ObservableProperty] private string _moduloIdTexto = string.Empty;
        [ObservableProperty] private string _aulaIdTexto = string.Empty;
        [ObservableProperty] private ObservableCollection<AlternativaForm> _alternativas = new();

        public QuestionBankViewModel(IAssessmentApiService service, IDialogService dialog)
        {
            _service = service;
            _dialog = dialog;
            NovaQuestaoInterno();
        }

        public bool Editando => EmEdicaoId.HasValue;
        public string TituloFormulario => Editando ? $"Editar questão #{EmEdicaoId}" : "Nova questão";
        partial void OnEmEdicaoIdChanged(int? value)
        {
            OnPropertyChanged(nameof(Editando));
            OnPropertyChanged(nameof(TituloFormulario));
        }

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
            PagedResult<QuestaoDto>? resultado = null;
            var ok = await RunAsync(async () => resultado = await _service.GetQuestoesAsync(
                string.IsNullOrWhiteSpace(FiltroBusca) ? null : FiltroBusca.Trim(),
                FiltroStatus == Todos ? null : FiltroStatus,
                FiltroDificuldade == Todos ? null : FiltroDificuldade,
                Pagina, TamanhoPagina), "Carregando questões...");
            if (!ok || resultado == null) return;

            Itens = new ObservableCollection<QuestaoDto>(resultado.Items);
            TotalItens = resultado.Total;
            TotalPaginas = resultado.TotalPages;
            SetStatus(Itens.Count == 0 ? "Nenhuma questão encontrada para os filtros informados." : string.Empty);
        }

        // ------------------------------------------------------------------ Formulário

        [RelayCommand]
        private void NovaQuestao() => NovaQuestaoInterno();

        private void NovaQuestaoInterno()
        {
            EmEdicaoId = null;
            Enunciado = string.Empty;
            Tipo = "MULTIPLA_ESCOLHA";
            Dificuldade = "MEDIO";
            PesoTexto = "1";
            ExplicacaoGeral = string.Empty;
            Assunto = string.Empty;
            TagsTexto = string.Empty;
            TrilhaIdTexto = string.Empty;
            ModuloIdTexto = string.Empty;
            AulaIdTexto = string.Empty;
            Alternativas = new ObservableCollection<AlternativaForm> { new(), new() };
        }

        [RelayCommand]
        private void EditarQuestao()
        {
            var q = SelectedItem;
            if (q == null) { SetStatus("Selecione uma questão na tabela para editar.", true); return; }

            EmEdicaoId = q.QuestaoID;
            Enunciado = q.Enunciado;
            Tipo = q.Tipo;
            Dificuldade = q.Dificuldade;
            PesoTexto = q.Peso.ToString();
            ExplicacaoGeral = q.Explicacao ?? string.Empty;
            Assunto = q.Assunto ?? string.Empty;
            TagsTexto = string.Join(", ", q.Tags ?? new List<string>());
            TrilhaIdTexto = q.TrilhaID?.ToString() ?? string.Empty;
            ModuloIdTexto = q.ModuloID?.ToString() ?? string.Empty;
            AulaIdTexto = q.AulaID?.ToString() ?? string.Empty;
            Alternativas = new ObservableCollection<AlternativaForm>((q.Alternativas ?? new List<AlternativaDto>()).Select(AlternativaForm.From));
            SetStatus($"Editando a questão #{q.QuestaoID}. Salve para aplicar as alterações.");
        }

        [RelayCommand]
        private void AdicionarAlternativa()
        {
            if (Alternativas.Count >= MaxAlternativas)
            {
                SetStatus($"Uma questão aceita no máximo {MaxAlternativas} alternativas.", true);
                return;
            }
            Alternativas.Add(new AlternativaForm());
        }

        [RelayCommand]
        private void RemoverAlternativa(AlternativaForm? alternativa)
        {
            if (alternativa == null) return;
            if (Alternativas.Count <= 2)
            {
                SetStatus("Uma questão precisa de pelo menos 2 alternativas.", true);
                return;
            }
            Alternativas.Remove(alternativa);
        }

        /// <summary>Valida o formulário e monta o DTO. Retorna a mensagem de erro ou null.</summary>
        public string? Validar(out QuestaoRequestDto? dto)
        {
            dto = null;
            var enunciado = Enunciado?.Trim() ?? string.Empty;
            if (enunciado.Length < 5 || enunciado.Length > 10000) return "O enunciado deve ter entre 5 e 10000 caracteres.";
            if (!int.TryParse(PesoTexto?.Trim(), out var peso) || peso < 1 || peso > 100) return "O peso deve ser um número entre 1 e 100.";
            if ((ExplicacaoGeral?.Length ?? 0) > 2000) return "A explicação geral deve ter no máximo 2000 caracteres.";
            if ((Assunto?.Length ?? 0) > 150) return "O assunto deve ter no máximo 150 caracteres.";

            var tags = (TagsTexto ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (tags.Count > 10) return "Use no máximo 10 tags.";

            if (!TryOptionalId(TrilhaIdTexto, out var trilhaId)) return "O ID da trilha deve ser um número maior que zero (ou fique em branco).";
            if (!TryOptionalId(ModuloIdTexto, out var moduloId)) return "O ID do módulo deve ser um número maior que zero (ou fique em branco).";
            if (!TryOptionalId(AulaIdTexto, out var aulaId)) return "O ID da aula deve ser um número maior que zero (ou fique em branco).";

            if (Alternativas.Count < 2 || Alternativas.Count > MaxAlternativas) return $"Informe de 2 a {MaxAlternativas} alternativas.";
            if (Alternativas.Any(a => string.IsNullOrWhiteSpace(a.Texto))) return "Todas as alternativas precisam de texto.";
            if (Alternativas.Any(a => a.Texto.Trim().Length > 2000)) return "O texto de cada alternativa deve ter no máximo 2000 caracteres.";
            if (Alternativas.Any(a => (a.Explicacao?.Length ?? 0) > 1000)) return "A explicação de cada alternativa deve ter no máximo 1000 caracteres.";
            if (!Alternativas.Any(a => a.Correta)) return "Marque pelo menos uma alternativa correta.";
            if (Tipo == "VERDADEIRO_FALSO" && Alternativas.Count != 2) return "Questões de verdadeiro ou falso devem ter exatamente 2 alternativas.";

            dto = new QuestaoRequestDto
            {
                Enunciado = enunciado,
                Tipo = Tipo,
                Dificuldade = Dificuldade,
                Peso = peso,
                Explicacao = string.IsNullOrWhiteSpace(ExplicacaoGeral) ? null : ExplicacaoGeral.Trim(),
                Assunto = string.IsNullOrWhiteSpace(Assunto) ? null : Assunto.Trim(),
                Tags = tags,
                TrilhaID = trilhaId,
                ModuloID = moduloId,
                AulaID = aulaId,
                Alternativas = Alternativas.Select(a => a.ToRequest()).ToList()
            };
            return null;
        }

        private static bool TryOptionalId(string? texto, out int? id)
        {
            id = null;
            if (string.IsNullOrWhiteSpace(texto)) return true;
            if (int.TryParse(texto.Trim(), out var valor) && valor >= 1) { id = valor; return true; }
            return false;
        }

        [RelayCommand]
        private async Task SalvarAsync()
        {
            var erro = Validar(out var dto);
            if (erro != null || dto == null) { SetStatus(erro ?? "Dados inválidos.", true); return; }

            var editando = EmEdicaoId;
            var ok = await RunAsync(() => editando.HasValue
                ? _service.UpdateQuestaoAsync(editando.Value, dto)
                : _service.CreateQuestaoAsync(dto));
            if (!ok) return;

            NovaQuestaoInterno();
            await CarregarAsync();
            if (!HasError) SetStatus(editando.HasValue ? "Questão atualizada (volta a RASCUNHO até nova revisão)." : "Questão criada como RASCUNHO.");
        }

        [RelayCommand]
        private async Task ExcluirAsync()
        {
            var alvo = SelectedItem;
            if (alvo == null) { SetStatus("Selecione uma questão na tabela para excluir.", true); return; }
            if (!_dialog.ShowConfirmation("Confirmar exclusão", $"Deseja excluir a questão #{alvo.QuestaoID}?\nEsta ação não pode ser desfeita.")) return;

            var ok = await RunAsync(() => _service.DeleteQuestaoAsync(alvo.QuestaoID));
            if (!ok) return;

            if (EmEdicaoId == alvo.QuestaoID) NovaQuestaoInterno();
            await CarregarAsync();
            if (!HasError) SetStatus($"Questão #{alvo.QuestaoID} excluída.");
        }

        [RelayCommand]
        private async Task EnviarRevisaoAsync()
        {
            var alvo = SelectedItem;
            if (alvo == null) { SetStatus("Selecione uma questão na tabela para enviar à revisão.", true); return; }

            var ok = await RunAsync(() => _service.SendQuestaoToReviewAsync(alvo.QuestaoID));
            if (!ok) return;

            await CarregarAsync();
            if (!HasError) SetStatus($"Questão #{alvo.QuestaoID} enviada para revisão.");
        }
    }
}
