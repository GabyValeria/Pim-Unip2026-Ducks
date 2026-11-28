using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels.Forms
{
    /// <summary>Formulário de desafio. Regras espelham o DesafioDto e a validação do controller.</summary>
    public partial class ChallengeForm : ObservableObject
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
        private int _recompensaXp;

        [ObservableProperty]
        private DateTime? _inicioEm;

        [ObservableProperty]
        private DateTime? _fimEm;

        [ObservableProperty]
        private bool _ativo = true;

        [ObservableProperty]
        private string _tipoRequisito = "NENHUM";

        [ObservableProperty]
        private int _metaRequisito;

        [ObservableProperty]
        private int? _trilhaId;

        [ObservableProperty]
        private int? _conquistaId;

        public bool IsEdicao => Id > 0;
        public string Titulo => IsEdicao ? "Editar desafio" : "Novo desafio";

        public static ChallengeForm From(ChallengeDto dto) => new()
        {
            Id = dto.DesafioID,
            Nome = dto.Nome,
            Descricao = dto.Descricao,
            RecompensaXp = dto.RecompensaXP,
            InicioEm = dto.InicioEm,
            FimEm = dto.FimEm,
            Ativo = dto.Ativo,
            TipoRequisito = string.IsNullOrWhiteSpace(dto.TipoRequisito) ? "NENHUM" : dto.TipoRequisito!.ToUpperInvariant(),
            MetaRequisito = dto.MetaRequisito,
            TrilhaId = dto.TrilhaID,
            ConquistaId = dto.ConquistaID
        };

        public string? Validate()
        {
            var nome = Nome?.Trim() ?? string.Empty;
            if (nome.Length < 2 || nome.Length > 120) return "O nome do desafio deve ter entre 2 e 120 caracteres.";
            if ((Descricao?.Length ?? 0) > 1000) return "A descrição deve ter no máximo 1000 caracteres.";
            if (RecompensaXp < 0) return "A recompensa de XP não pode ser negativa.";
            if (InicioEm.HasValue && FimEm.HasValue && FimEm <= InicioEm) return "A data de fim deve ser posterior à data de início.";

            var tipo = (TipoRequisito ?? "NENHUM").Trim().ToUpperInvariant();
            if (!TiposRequisito.Todos.Contains(tipo)) return "Tipo de requisito inválido.";
            if (MetaRequisito < 0 || MetaRequisito > 100000) return "A meta do requisito deve estar entre 0 e 100000.";
            if (tipo != "NENHUM" && MetaRequisito < 1) return "Informe a meta do requisito (mínimo 1) para este tipo de requisito.";
            if (TrilhaId is < 1) return "O ID da trilha deve ser maior que zero.";
            if (ConquistaId is < 1) return "O ID da medalha deve ser maior que zero.";
            return null;
        }

        public ChallengeRequestDto ToRequest() => new()
        {
            Nome = Nome.Trim(),
            Descricao = (Descricao ?? string.Empty).Trim(),
            RecompensaXP = RecompensaXp,
            InicioEm = InicioEm,
            FimEm = FimEm,
            Ativo = Ativo,
            TipoRequisito = (TipoRequisito ?? "NENHUM").Trim().ToUpperInvariant(),
            MetaRequisito = MetaRequisito,
            TrilhaID = TrilhaId,
            ConquistaID = ConquistaId
        };
    }
}
