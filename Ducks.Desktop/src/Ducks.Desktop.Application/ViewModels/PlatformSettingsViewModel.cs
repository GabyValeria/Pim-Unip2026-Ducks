using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels
{
    public partial class PlatformSettingsViewModel : ViewModelBase
    {
        // A API recusa chaves sensíveis; validamos antes para dar retorno imediato.
        private static readonly string[] TermosSensiveis = { "key", "secret", "password", "token", "connection" };

        private readonly ISettingsApiService _settingsService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private ObservableCollection<SettingDto> _parametros = new();

        [ObservableProperty]
        private SettingDto? _selectedParametro;

        [ObservableProperty]
        private string _chave = string.Empty;

        [ObservableProperty]
        private string _valor = string.Empty;

        public PlatformSettingsViewModel(ISettingsApiService settingsService, IDialogService dialogService)
        {
            _settingsService = settingsService;
            _dialogService = dialogService;
        }

        public override Task InitializeAsync() => CarregarParametrosAsync();

        partial void OnSelectedParametroChanged(SettingDto? value)
        {
            if (value == null) return;
            Chave = value.Chave;
            Valor = value.Valor;
        }

        [RelayCommand]
        private Task AtualizarAsync() => CarregarParametrosAsync();

        [RelayCommand]
        private void NovoParametro()
        {
            SelectedParametro = null;
            Chave = string.Empty;
            Valor = string.Empty;
            SetStatus(string.Empty);
        }

        public async Task CarregarParametrosAsync()
        {
            System.Collections.Generic.IReadOnlyList<SettingDto>? lista = null;
            var ok = await RunAsync(async () => lista = await _settingsService.GetSettingsAsync(), "Carregando parâmetros globais...");
            if (!ok || lista == null) return;

            Parametros = new ObservableCollection<SettingDto>(lista);
            SetStatus(Parametros.Count == 0 ? "Nenhum parâmetro cadastrado." : string.Empty);
        }

        public string? ValidarEntrada()
        {
            var chave = Chave?.Trim() ?? string.Empty;
            if (chave.Length < 2 || chave.Length > 100) return "A chave deve ter entre 2 e 100 caracteres.";
            if (TermosSensiveis.Any(t => chave.Contains(t, StringComparison.OrdinalIgnoreCase)))
                return "Credenciais e segredos não podem ser alterados aqui. Use variáveis de ambiente, User Secrets ou Key Vault.";
            if (string.IsNullOrWhiteSpace(Valor)) return "Informe o valor do parâmetro.";
            if (Valor.Length > 4000) return "O valor deve ter no máximo 4000 caracteres.";
            return null;
        }

        [RelayCommand]
        private async Task SalvarParametroAsync()
        {
            var erro = ValidarEntrada();
            if (erro != null)
            {
                SetStatus(erro, true);
                return;
            }

            var chave = Chave.Trim();
            var existe = Parametros.Any(p => string.Equals(p.Chave, chave, StringComparison.OrdinalIgnoreCase));
            var mensagem = existe
                ? $"Atualizar o parâmetro '{chave}'? A alteração afeta toda a plataforma."
                : $"Criar o novo parâmetro '{chave}'?";

            if (!_dialogService.ShowConfirmation("Confirmar alteração", mensagem)) return;

            var sucesso = await RunAsync(() => _settingsService.SaveSettingAsync(chave, Valor));
            if (!sucesso) return;

            await CarregarParametrosAsync();
            if (!HasError) SetStatus($"Parâmetro '{chave}' salvo com sucesso.");
        }
    }
}
