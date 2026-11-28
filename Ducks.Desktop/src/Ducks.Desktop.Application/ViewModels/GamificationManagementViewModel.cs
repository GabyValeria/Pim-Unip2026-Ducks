using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Application.ViewModels.Forms;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels
{
    public partial class GamificationManagementViewModel : ViewModelBase
    {
        private readonly IGamificationApiService _service;
        private readonly IDialogService _dialogService;

        [ObservableProperty] private ObservableCollection<BadgeDto> _badges = new();
        [ObservableProperty] private BadgeDto? _selectedBadge;
        [ObservableProperty] private BadgeForm _badgeForm = new();

        [ObservableProperty] private ObservableCollection<ChallengeDto> _desafios = new();
        [ObservableProperty] private ChallengeDto? _selectedDesafio;
        [ObservableProperty] private ChallengeForm _desafioForm = new();

        [ObservableProperty] private int _usuarioConsultaId;
        [ObservableProperty] private ObservableCollection<UserBadgeDto> _medalhasDoUsuario = new();

        public IReadOnlyList<string> TiposRequisitoDisponiveis => TiposRequisito.Todos;

        public GamificationManagementViewModel(IGamificationApiService service, IDialogService dialogService)
        {
            _service = service;
            _dialogService = dialogService;
        }

        public override Task InitializeAsync() => CarregarDadosAsync();

        partial void OnSelectedBadgeChanged(BadgeDto? value)
        {
            if (value != null) BadgeForm = BadgeForm.From(value);
        }

        partial void OnSelectedDesafioChanged(ChallengeDto? value)
        {
            if (value != null) DesafioForm = ChallengeForm.From(value);
        }

        [RelayCommand]
        private Task AtualizarAsync() => CarregarDadosAsync();

        public async Task CarregarDadosAsync()
        {
            IReadOnlyList<BadgeDto>? badges = null;
            IReadOnlyList<ChallengeDto>? desafios = null;

            var ok = await RunAsync(async () =>
            {
                badges = await _service.GetBadgesAsync();
                desafios = await _service.GetChallengesAsync();
            }, "Carregando medalhas e desafios...");

            if (!ok) return;

            Badges = new ObservableCollection<BadgeDto>(badges ?? new List<BadgeDto>());
            Desafios = new ObservableCollection<ChallengeDto>(desafios ?? new List<ChallengeDto>());
        }

        // ---------------- Medalhas ----------------

        [RelayCommand]
        private void NovaMedalha()
        {
            SelectedBadge = null;
            BadgeForm = new BadgeForm();
            SetStatus(string.Empty);
        }

        [RelayCommand]
        private async Task SalvarMedalhaAsync()
        {
            var erro = BadgeForm.Validate();
            if (erro != null) { SetStatus(erro, true); return; }

            var form = BadgeForm;
            var sucesso = await RunAsync(() => form.IsEdicao
                ? _service.UpdateBadgeAsync(form.Id, form.ToRequest())
                : _service.CreateBadgeAsync(form.ToRequest()));
            if (!sucesso) return;

            var mensagem = form.IsEdicao ? "Medalha atualizada com sucesso." : "Medalha criada com sucesso.";
            await CarregarDadosAsync();
            if (HasError) return;

            NovaMedalha();
            SetStatus(mensagem);
        }

        [RelayCommand]
        private async Task ExcluirMedalhaAsync()
        {
            var alvo = SelectedBadge;
            if (alvo == null) { SetStatus("Selecione uma medalha na tabela para excluir.", true); return; }

            var aviso = alvo.TotalConcedidas > 0
                ? $"A medalha '{alvo.Nome}' já foi concedida a {alvo.TotalConcedidas} usuário(s). Deseja realmente excluí-la?"
                : $"Deseja excluir a medalha '{alvo.Nome}'?";
            if (!_dialogService.ShowConfirmation("Confirmar exclusão", aviso)) return;

            var sucesso = await RunAsync(() => _service.DeleteBadgeAsync(alvo.ConquistaID));
            if (!sucesso) return;

            await CarregarDadosAsync();
            if (HasError) return;

            NovaMedalha();
            SetStatus($"Medalha '{alvo.Nome}' excluída.");
        }

        // ---------------- Desafios ----------------

        [RelayCommand]
        private void NovoDesafio()
        {
            SelectedDesafio = null;
            DesafioForm = new ChallengeForm();
            SetStatus(string.Empty);
        }

        [RelayCommand]
        private async Task SalvarDesafioAsync()
        {
            var erro = DesafioForm.Validate();
            if (erro != null) { SetStatus(erro, true); return; }

            var form = DesafioForm;
            var sucesso = await RunAsync(() => form.IsEdicao
                ? _service.UpdateChallengeAsync(form.Id, form.ToRequest())
                : _service.CreateChallengeAsync(form.ToRequest()));
            if (!sucesso) return;

            var mensagem = form.IsEdicao ? "Desafio atualizado com sucesso." : "Desafio criado com sucesso.";
            await CarregarDadosAsync();
            if (HasError) return;

            NovoDesafio();
            SetStatus(mensagem);
        }

        [RelayCommand]
        private async Task ExcluirDesafioAsync()
        {
            var alvo = SelectedDesafio;
            if (alvo == null) { SetStatus("Selecione um desafio na tabela para excluir.", true); return; }

            if (!_dialogService.ShowConfirmation("Confirmar exclusão", $"Deseja excluir o desafio '{alvo.Nome}'?")) return;

            var sucesso = await RunAsync(() => _service.DeleteChallengeAsync(alvo.DesafioID));
            if (!sucesso) return;

            await CarregarDadosAsync();
            if (HasError) return;

            NovoDesafio();
            SetStatus($"Desafio '{alvo.Nome}' excluído.");
        }

        // ---------------- Medalhas por usuário ----------------

        [RelayCommand]
        private async Task ConsultarMedalhasUsuarioAsync()
        {
            if (UsuarioConsultaId <= 0)
            {
                SetStatus("Informe um ID de usuário válido (maior que zero).", true);
                return;
            }

            IReadOnlyList<UserBadgeDto>? lista = null;
            var ok = await RunAsync(async () => lista = await _service.GetUserBadgesAsync(UsuarioConsultaId));
            if (!ok) return;

            MedalhasDoUsuario = new ObservableCollection<UserBadgeDto>(lista ?? new List<UserBadgeDto>());
            SetStatus(MedalhasDoUsuario.Count == 0
                ? $"O usuário {UsuarioConsultaId} ainda não possui medalhas."
                : $"{MedalhasDoUsuario.Count} medalha(s) do usuário {UsuarioConsultaId}.");
        }
    }
}
