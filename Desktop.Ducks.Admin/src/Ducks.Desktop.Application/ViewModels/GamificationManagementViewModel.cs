using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels
{
    public partial class GamificationManagementViewModel : ObservableObject
    {
        private readonly IGamificationApiService _gamificationService;

        [ObservableProperty]
        private ObservableCollection<BadgeDto> _badges = new();

        [ObservableProperty]
        private ObservableCollection<ChallengeDto> _desafios = new();

        [ObservableProperty]
        private int _targetUsuarioId;

        [ObservableProperty]
        private int _quantidadeXpAjuste;

        [ObservableProperty]
        private string _motivoAjuste = string.Empty;

        [ObservableProperty]
        private string _mensagemStatus = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public IAsyncRelayCommand CarregarDadosCommand { get; }
        public IAsyncRelayCommand AjustarXpCommand { get; }

        public GamificationManagementViewModel(IGamificationApiService gamificationService)
        {
            _gamificationService = gamificationService;
            CarregarDadosCommand = new AsyncRelayCommand(CarregarDadosAsync);
            AjustarXpCommand = new AsyncRelayCommand(AjustarXpAsync);

            // Dispara o carregamento automático ao abrir a tela
            _ = CarregarDadosAsync();
        }

        public async Task CarregarDadosAsync()
        {
            IsLoading = true;
            try
            {
                var badgesList = await _gamificationService.GetBadgesAsync();
                Badges = new ObservableCollection<BadgeDto>(badgesList);

                var challengesList = await _gamificationService.GetChallengesAsync();
                Desafios = new ObservableCollection<ChallengeDto>(challengesList);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AjustarXpAsync()
        {
            if (TargetUsuarioId <= 0 || QuantidadeXpAjuste == 0)
            {
                MensagemStatus = "Informe um ID de usuário válido e uma quantidade de XP diferente de zero.";
                return;
            }

            var dto = new UserXpAdjustmentDto
            {
                UsuarioID = TargetUsuarioId,
                QuantidadeXP = QuantidadeXpAjuste,
                Motivo = string.IsNullOrWhiteSpace(MotivoAjuste) ? "Ajuste administrativo manual" : MotivoAjuste
            };

            bool sucesso = await _gamificationService.AdjustUserXpAsync(dto);
            if (sucesso)
            {
                MensagemStatus = $"XP ajustado com sucesso para o usuário {TargetUsuarioId}!";
                TargetUsuarioId = 0;
                QuantidadeXpAjuste = 0;
                MotivoAjuste = string.Empty;
            }
            else
            {
                MensagemStatus = "Falha ao processar o ajuste de XP na API.";
            }
        }
    }
}