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
    /// <summary>Gestão de módulos e aulas de uma trilha (criar, editar, excluir, reordenar e enviar para revisão).</summary>
    public partial class ContentStructureViewModel : ViewModelBase
    {
        private readonly IContentApiService _trilhaService;
        private readonly IContentManagementApiService _service;
        private readonly IDialogService _dialog;

        [ObservableProperty] private ObservableCollection<TrilhaDto> _trilhas = new();
        [ObservableProperty] private TrilhaDto? _selectedTrilha;
        [ObservableProperty] private ObservableCollection<ModuloDto> _modulos = new();
        [ObservableProperty] private ModuloDto? _selectedModulo;
        [ObservableProperty] private ObservableCollection<AulaDto> _aulas = new();
        [ObservableProperty] private AulaDto? _selectedAula;

        // Formulário de módulo
        [ObservableProperty] private string _moduloTitulo = string.Empty;
        [ObservableProperty] private string _moduloDescricao = string.Empty;
        [ObservableProperty] private int? _moduloEmEdicaoId;

        // Formulário de aula
        [ObservableProperty] private string _aulaTitulo = string.Empty;
        [ObservableProperty] private string _aulaUrl = string.Empty;
        [ObservableProperty] private string _aulaConteudo = string.Empty;
        [ObservableProperty] private int? _aulaEmEdicaoId;

        public ContentStructureViewModel(IContentApiService trilhaService, IContentManagementApiService service, IDialogService dialog)
        {
            _trilhaService = trilhaService;
            _service = service;
            _dialog = dialog;
        }

        public bool EditandoModulo => ModuloEmEdicaoId.HasValue;
        public bool EditandoAula => AulaEmEdicaoId.HasValue;

        partial void OnModuloEmEdicaoIdChanged(int? value) => OnPropertyChanged(nameof(EditandoModulo));
        partial void OnAulaEmEdicaoIdChanged(int? value) => OnPropertyChanged(nameof(EditandoAula));
        partial void OnSelectedTrilhaChanged(TrilhaDto? value) => _ = CarregarModulosAsync();
        partial void OnSelectedModuloChanged(ModuloDto? value) => _ = CarregarAulasAsync();

        public override async Task InitializeAsync()
        {
            IReadOnlyList<TrilhaDto>? lista = null;
            var ok = await RunAsync(async () => lista = await _trilhaService.GetTrilhasAsync(), "Carregando trilhas...");
            if (!ok || lista == null) return;

            Trilhas = new ObservableCollection<TrilhaDto>(lista);
            SetStatus(Trilhas.Count == 0 ? "Nenhuma trilha cadastrada. Crie uma em Cursos e Trilhas." : "Selecione uma trilha para ver seus módulos.");
        }

        [RelayCommand]
        private Task AtualizarAsync() => SelectedTrilha == null ? InitializeAsync() : CarregarModulosAsync();

        public async Task CarregarModulosAsync()
        {
            var trilha = SelectedTrilha;
            Modulos = new ObservableCollection<ModuloDto>();
            SelectedModulo = null;
            Aulas = new ObservableCollection<AulaDto>();
            LimparFormularioModulo();
            LimparFormularioAula();
            if (trilha == null) return;

            IReadOnlyList<ModuloDto>? lista = null;
            var ok = await RunAsync(async () => lista = await _service.GetModulosAsync(trilha.TrilhaID), "Carregando módulos...");
            if (!ok || lista == null) return;

            Modulos = new ObservableCollection<ModuloDto>(lista.OrderBy(m => m.IndiceOrdem));
            SetStatus(Modulos.Count == 0 ? "Esta trilha ainda não tem módulos." : string.Empty);
        }

        public async Task CarregarAulasAsync()
        {
            var modulo = SelectedModulo;
            Aulas = new ObservableCollection<AulaDto>();
            LimparFormularioAula();
            if (modulo == null) return;

            IReadOnlyList<AulaDto>? lista = null;
            var ok = await RunAsync(async () => lista = await _service.GetAulasAsync(modulo.ModuloID), "Carregando aulas...");
            if (!ok || lista == null) return;

            Aulas = new ObservableCollection<AulaDto>(lista.OrderBy(a => a.IndiceOrdem));
            SetStatus(Aulas.Count == 0 ? "Este módulo ainda não tem aulas." : string.Empty);
        }

        // ------------------------------------------------------------------ Validação

        public string? ValidarModulo()
        {
            var titulo = ModuloTitulo?.Trim() ?? string.Empty;
            if (titulo.Length < 3 || titulo.Length > 150) return "O título do módulo deve ter entre 3 e 150 caracteres.";
            if ((ModuloDescricao?.Length ?? 0) > 2000) return "A descrição do módulo deve ter no máximo 2000 caracteres.";
            return null;
        }

        public string? ValidarAula()
        {
            var titulo = AulaTitulo?.Trim() ?? string.Empty;
            if (titulo.Length < 3 || titulo.Length > 150) return "O título da aula deve ter entre 3 e 150 caracteres.";
            if ((AulaUrl?.Length ?? 0) > 500) return "A URL do conteúdo deve ter no máximo 500 caracteres.";
            if ((AulaConteudo?.Length ?? 0) > 20000) return "O texto da aula deve ter no máximo 20000 caracteres.";
            return null;
        }

        // ------------------------------------------------------------------ Módulos

        [RelayCommand]
        private void NovoModulo() => LimparFormularioModulo();

        [RelayCommand]
        private void EditarModulo()
        {
            var alvo = SelectedModulo;
            if (alvo == null) { SetStatus("Selecione um módulo na tabela para editar.", true); return; }
            ModuloEmEdicaoId = alvo.ModuloID;
            ModuloTitulo = alvo.Titulo;
            ModuloDescricao = alvo.Descricao ?? string.Empty;
            SetStatus($"Editando o módulo '{alvo.Titulo}'. Salve para aplicar as alterações.");
        }

        [RelayCommand]
        private async Task SalvarModuloAsync()
        {
            var trilha = SelectedTrilha;
            if (trilha == null) { SetStatus("Selecione uma trilha antes de salvar um módulo.", true); return; }
            var erro = ValidarModulo();
            if (erro != null) { SetStatus(erro, true); return; }

            var dto = new ModuloRequestDto
            {
                Titulo = ModuloTitulo.Trim(),
                Descricao = string.IsNullOrWhiteSpace(ModuloDescricao) ? null : ModuloDescricao.Trim()
            };

            var editando = ModuloEmEdicaoId;
            var ok = await RunAsync(() => editando.HasValue
                ? _service.UpdateModuloAsync(editando.Value, dto)
                : _service.CreateModuloAsync(trilha.TrilhaID, dto));
            if (!ok) return;

            var mensagem = editando.HasValue ? $"Módulo '{dto.Titulo}' atualizado (volta a RASCUNHO até nova revisão)." : $"Módulo '{dto.Titulo}' criado como RASCUNHO.";
            await CarregarModulosAsync();
            if (!HasError) SetStatus(mensagem);
        }

        [RelayCommand]
        private async Task ExcluirModuloAsync()
        {
            var alvo = SelectedModulo;
            if (alvo == null) { SetStatus("Selecione um módulo na tabela para excluir.", true); return; }
            if (!_dialog.ShowConfirmation("Confirmar exclusão",
                    $"Deseja excluir o módulo '{alvo.Titulo}'?\nAs aulas dele também podem ser afetadas. Esta ação não pode ser desfeita.")) return;

            var ok = await RunAsync(() => _service.DeleteModuloAsync(alvo.ModuloID));
            if (!ok) return;

            await CarregarModulosAsync();
            if (!HasError) SetStatus($"Módulo '{alvo.Titulo}' excluído.");
        }

        [RelayCommand]
        private async Task EnviarModuloRevisaoAsync()
        {
            var alvo = SelectedModulo;
            if (alvo == null) { SetStatus("Selecione um módulo na tabela para enviar à revisão.", true); return; }

            var ok = await RunAsync(() => _service.SendModuloToReviewAsync(alvo.ModuloID));
            if (!ok) return;

            await CarregarModulosAsync();
            if (!HasError) SetStatus($"Módulo '{alvo.Titulo}' enviado para revisão.");
        }

        [RelayCommand]
        private Task SubirModuloAsync() => MoverModuloAsync(-1);

        [RelayCommand]
        private Task DescerModuloAsync() => MoverModuloAsync(1);

        private async Task MoverModuloAsync(int delta)
        {
            var trilha = SelectedTrilha;
            var alvo = SelectedModulo;
            if (trilha == null || alvo == null) { SetStatus("Selecione um módulo na tabela para mudar a ordem.", true); return; }

            var ids = ReordenarIds(Modulos.Select(m => m.ModuloID).ToList(), alvo.ModuloID, delta);
            if (ids == null) return;

            var ok = await RunAsync(() => _service.ReorderModulosAsync(trilha.TrilhaID, ids));
            if (!ok) return;

            await CarregarModulosAsync();
            SelectedModulo = Modulos.FirstOrDefault(m => m.ModuloID == alvo.ModuloID);
        }

        // ------------------------------------------------------------------ Aulas

        [RelayCommand]
        private void NovaAula() => LimparFormularioAula();

        [RelayCommand]
        private void EditarAula()
        {
            var alvo = SelectedAula;
            if (alvo == null) { SetStatus("Selecione uma aula na tabela para editar.", true); return; }
            AulaEmEdicaoId = alvo.AulaID;
            AulaTitulo = alvo.Titulo;
            AulaUrl = alvo.UrlConteudo ?? string.Empty;
            AulaConteudo = alvo.Conteudo ?? string.Empty;
            SetStatus($"Editando a aula '{alvo.Titulo}'. Salve para aplicar as alterações.");
        }

        [RelayCommand]
        private async Task SalvarAulaAsync()
        {
            var modulo = SelectedModulo;
            if (modulo == null) { SetStatus("Selecione um módulo antes de salvar uma aula.", true); return; }
            var erro = ValidarAula();
            if (erro != null) { SetStatus(erro, true); return; }

            var dto = new AulaRequestDto
            {
                Titulo = AulaTitulo.Trim(),
                UrlConteudo = (AulaUrl ?? string.Empty).Trim(),
                Conteudo = string.IsNullOrWhiteSpace(AulaConteudo) ? null : AulaConteudo
            };

            var editando = AulaEmEdicaoId;
            var ok = await RunAsync(() => editando.HasValue
                ? _service.UpdateAulaAsync(editando.Value, dto)
                : _service.CreateAulaAsync(modulo.ModuloID, dto));
            if (!ok) return;

            var mensagem = editando.HasValue ? $"Aula '{dto.Titulo}' atualizada (volta a RASCUNHO até nova revisão)." : $"Aula '{dto.Titulo}' criada como RASCUNHO.";
            await CarregarAulasAsync();
            if (!HasError) SetStatus(mensagem);
        }

        [RelayCommand]
        private async Task ExcluirAulaAsync()
        {
            var alvo = SelectedAula;
            if (alvo == null) { SetStatus("Selecione uma aula na tabela para excluir.", true); return; }
            if (!_dialog.ShowConfirmation("Confirmar exclusão",
                    $"Deseja excluir a aula '{alvo.Titulo}'?\nEsta ação não pode ser desfeita.")) return;

            var ok = await RunAsync(() => _service.DeleteAulaAsync(alvo.AulaID));
            if (!ok) return;

            await CarregarAulasAsync();
            if (!HasError) SetStatus($"Aula '{alvo.Titulo}' excluída.");
        }

        [RelayCommand]
        private async Task EnviarAulaRevisaoAsync()
        {
            var alvo = SelectedAula;
            if (alvo == null) { SetStatus("Selecione uma aula na tabela para enviar à revisão.", true); return; }

            var ok = await RunAsync(() => _service.SendAulaToReviewAsync(alvo.AulaID));
            if (!ok) return;

            await CarregarAulasAsync();
            if (!HasError) SetStatus($"Aula '{alvo.Titulo}' enviada para revisão.");
        }

        [RelayCommand]
        private Task SubirAulaAsync() => MoverAulaAsync(-1);

        [RelayCommand]
        private Task DescerAulaAsync() => MoverAulaAsync(1);

        private async Task MoverAulaAsync(int delta)
        {
            var modulo = SelectedModulo;
            var alvo = SelectedAula;
            if (modulo == null || alvo == null) { SetStatus("Selecione uma aula na tabela para mudar a ordem.", true); return; }

            var ids = ReordenarIds(Aulas.Select(a => a.AulaID).ToList(), alvo.AulaID, delta);
            if (ids == null) return;

            var ok = await RunAsync(() => _service.ReorderAulasAsync(modulo.ModuloID, ids));
            if (!ok) return;

            await CarregarAulasAsync();
            SelectedAula = Aulas.FirstOrDefault(a => a.AulaID == alvo.AulaID);
        }

        // ------------------------------------------------------------------ Auxiliares

        /// <summary>Move o item uma posição para cima/baixo. Retorna null se já estiver no limite.</summary>
        public static List<int>? ReordenarIds(List<int> ids, int idAlvo, int delta)
        {
            var indice = ids.IndexOf(idAlvo);
            var destino = indice + delta;
            if (indice < 0 || destino < 0 || destino >= ids.Count) return null;

            ids.RemoveAt(indice);
            ids.Insert(destino, idAlvo);
            return ids;
        }

        private void LimparFormularioModulo()
        {
            ModuloEmEdicaoId = null;
            ModuloTitulo = string.Empty;
            ModuloDescricao = string.Empty;
        }

        private void LimparFormularioAula()
        {
            AulaEmEdicaoId = null;
            AulaTitulo = string.Empty;
            AulaUrl = string.Empty;
            AulaConteudo = string.Empty;
        }
    }
}
