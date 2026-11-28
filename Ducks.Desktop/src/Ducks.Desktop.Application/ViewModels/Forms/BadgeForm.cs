using CommunityToolkit.Mvvm.ComponentModel;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels.Forms
{
    /// <summary>Formulário de medalha. Regras espelham o ConquistaDto da API.</summary>
    public partial class BadgeForm : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEdicao))]
        [NotifyPropertyChangedFor(nameof(Titulo))]
        private int _id;

        [ObservableProperty]
        private string _nome = string.Empty;

        [ObservableProperty]
        private string _descricao = string.Empty;

        [ObservableProperty]
        private int _xpNecessario;

        [ObservableProperty]
        private bool _ativa = true;

        public bool IsEdicao => Id > 0;
        public string Titulo => IsEdicao ? "Editar medalha" : "Nova medalha";

        public static BadgeForm From(BadgeDto dto) => new()
        {
            Id = dto.ConquistaID,
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            XpNecessario = dto.XPNecessario,
            Ativa = dto.Ativa
        };

        public string? Validate()
        {
            var nome = Nome?.Trim() ?? string.Empty;
            if (nome.Length < 2 || nome.Length > 120) return "O nome da medalha deve ter entre 2 e 120 caracteres.";
            if ((Descricao?.Length ?? 0) > 1000) return "A descrição deve ter no máximo 1000 caracteres.";
            if (XpNecessario < 0) return "O XP necessário não pode ser negativo.";
            return null;
        }

        public BadgeRequestDto ToRequest() => new()
        {
            Nome = Nome.Trim(),
            Descricao = (Descricao ?? string.Empty).Trim(),
            XPNecessario = XpNecessario,
            Ativa = Ativa
        };
    }
}
