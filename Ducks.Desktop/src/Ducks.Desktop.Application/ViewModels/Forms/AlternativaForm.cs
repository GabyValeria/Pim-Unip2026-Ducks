using CommunityToolkit.Mvvm.ComponentModel;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels.Forms
{
    /// <summary>Linha editável de alternativa no formulário de questão.</summary>
    public partial class AlternativaForm : ObservableObject
    {
        [ObservableProperty] private string _texto = string.Empty;
        [ObservableProperty] private bool _correta;
        [ObservableProperty] private string _explicacao = string.Empty;

        /// <summary>Preenchido ao editar uma questão existente (a API mantém a mesma alternativa).</summary>
        public int? AlternativaID { get; set; }

        public static AlternativaForm From(AlternativaDto dto) => new()
        {
            AlternativaID = dto.AlternativaID,
            Texto = dto.Texto,
            Correta = dto.Correta,
            Explicacao = dto.Explicacao ?? string.Empty
        };

        public AlternativaRequestDto ToRequest() => new()
        {
            AlternativaID = AlternativaID,
            Texto = (Texto ?? string.Empty).Trim(),
            Correta = Correta,
            Explicacao = string.IsNullOrWhiteSpace(Explicacao) ? null : Explicacao.Trim()
        };
    }
}
