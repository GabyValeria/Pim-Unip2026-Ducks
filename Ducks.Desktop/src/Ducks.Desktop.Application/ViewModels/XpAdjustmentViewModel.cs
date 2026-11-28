using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels
{
    /// <summary>Concede ou remove XP de um usuário (POST api/educacional/xp/adicionar, somente Admin).</summary>
    public partial class XpAdjustmentViewModel : ViewModelBase
    {
        public const int LimiteXp = 10000;

        private readonly IXpApiService _service;
        private readonly IDialogService _dialog;

        [ObservableProperty] private string _usuarioIdTexto = string.Empty;
        [ObservableProperty] private string _quantidadeTexto = string.Empty;
        [ObservableProperty] private string _motivo = string.Empty;

        public XpAdjustmentViewModel(IXpApiService service, IDialogService dialog)
        {
            _service = service;
            _dialog = dialog;
        }

        /// <summary>Valida o formulário. Retorna a mensagem de erro, ou null se válido (e preenche o DTO).</summary>
        public string? Validar(out AdicionarXpRequestDto? dto)
        {
            dto = null;
            if (!int.TryParse(UsuarioIdTexto?.Trim(), out var usuarioId) || usuarioId < 1)
                return "Informe o ID do usuário (número maior que zero).";
            if (!int.TryParse(QuantidadeTexto?.Trim(), out var quantidade) || quantidade == 0)
                return "Informe a quantidade de XP (diferente de zero). Use valor negativo para remover XP.";
            if (quantidade < -LimiteXp || quantidade > LimiteXp)
                return $"A quantidade de XP deve estar entre -{LimiteXp} e {LimiteXp}.";
            var motivo = Motivo?.Trim() ?? string.Empty;
            if (motivo.Length < 3 || motivo.Length > 250)
                return "O motivo deve ter entre 3 e 250 caracteres.";

            dto = new AdicionarXpRequestDto { UsuarioID = usuarioId, QuantidadeXP = quantidade, Motivo = motivo };
            return null;
        }

        [RelayCommand]
        private async Task AplicarAsync()
        {
            var erro = Validar(out var dto);
            if (erro != null || dto == null) { SetStatus(erro ?? "Dados inválidos.", true); return; }

            var verbo = dto.QuantidadeXP > 0 ? "conceder" : "remover";
            if (!_dialog.ShowConfirmation("Confirmar ajuste de XP",
                    $"Deseja {verbo} {System.Math.Abs(dto.QuantidadeXP)} XP do usuário #{dto.UsuarioID}?\nMotivo: {dto.Motivo}")) return;

            var ok = await RunAsync(() => _service.AdjustXpAsync(dto));
            if (!ok) return;

            SetStatus($"Ajuste de {dto.QuantidadeXP:+#;-#;0} XP aplicado ao usuário #{dto.UsuarioID}.");
            QuantidadeTexto = string.Empty;
            Motivo = string.Empty;
        }
    }
}
