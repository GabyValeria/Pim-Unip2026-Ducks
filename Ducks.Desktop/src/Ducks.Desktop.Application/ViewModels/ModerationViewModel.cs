using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels
{
    public partial class ModerationViewModel : ViewModelBase, IDisposable
    {
        private readonly IModerationApiService _moderationService;
        private readonly IDialogService _dialogService;
        private SynchronizationContext? _uiContext;

        [ObservableProperty] private ObservableCollection<ModerationItemDto> _filaModeracao = new();
        [ObservableProperty] private ObservableCollection<ModerationItemDto> _historico = new();
        [ObservableProperty] private ModerationItemDto? _selectedItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ExibirFila))]
        private bool _exibirHistorico;

        [ObservableProperty] private bool _tempoRealAtivo;

        public bool ExibirFila => !ExibirHistorico;

        public ModerationViewModel(IModerationApiService moderationService, IDialogService dialogService)
        {
            _moderationService = moderationService;
            _dialogService = dialogService;
        }

        public override async Task InitializeAsync()
        {
            _uiContext = SynchronizationContext.Current;
            await CarregarFilaAsync();

            TempoRealAtivo = await _moderationService.TryStartRealtimeAsync(OnNovoItem);
        }

        private void OnNovoItem(ModerationItemDto item)
        {
            void Inserir() => FilaModeracao.Insert(0, item);
            if (_uiContext != null) _uiContext.Post(_ => Inserir(), null);
            else Inserir();
        }

        partial void OnExibirHistoricoChanged(bool value)
        {
            if (value) _ = CarregarHistoricoAsync();
        }

        [RelayCommand]
        private async Task AtualizarAsync()
        {
            await CarregarFilaAsync();
            if (ExibirHistorico) await CarregarHistoricoAsync();
        }

        public async Task CarregarFilaAsync()
        {
            IReadOnlyList<ModerationItemDto>? itens = null;
            var ok = await RunAsync(async () => itens = await _moderationService.GetPendingAsync(), "Carregando fila de moderação...");
            if (!ok || itens == null) return;

            FilaModeracao = new ObservableCollection<ModerationItemDto>(itens);
            SetStatus(FilaModeracao.Count == 0 ? "Nenhum item pendente de moderação." : $"{FilaModeracao.Count} item(ns) aguardando revisão.");
        }

        public async Task CarregarHistoricoAsync()
        {
            IReadOnlyList<ModerationItemDto>? itens = null;
            var ok = await RunAsync(async () => itens = await _moderationService.GetHistoryAsync(), "Carregando histórico...");
            if (!ok || itens == null) return;

            Historico = new ObservableCollection<ModerationItemDto>(itens);
            SetStatus(Historico.Count == 0 ? "Nenhuma decisão registrada ainda." : string.Empty);
        }

        private bool ItemValido(ModerationItemDto? item)
        {
            if (item == null) return false;
            if (!item.PodeAgir)
            {
                SetStatus("Não foi possível identificar o tipo/ID deste item. Atualize a fila e tente novamente.", true);
                return false;
            }
            return true;
        }

        [RelayCommand]
        private async Task AprovarAsync(ModerationItemDto? item)
        {
            if (!ItemValido(item)) return;

            var sucesso = await RunAsync(() => _moderationService.ApproveAsync(item!.Tipo, item.ConteudoID));
            if (!sucesso) return;

            FilaModeracao.Remove(item!);
            SetStatus($"'{item!.Titulo}' aprovado.");
        }

        [RelayCommand]
        private async Task RejeitarAsync(ModerationItemDto? item)
        {
            if (!ItemValido(item)) return;

            if (!_dialogService.ShowPrompt("Rejeitar conteúdo",
                    $"Informe a justificativa da rejeição de '{item!.Titulo}':", "Confirmar rejeição", out var justificativa)) return;

            var sucesso = await RunAsync(() => _moderationService.RejectAsync(item.Tipo, item.ConteudoID, justificativa));
            if (!sucesso) return;

            FilaModeracao.Remove(item);
            SetStatus($"'{item.Titulo}' rejeitado.");
        }

        [RelayCommand]
        private async Task RemoverAsync(ModerationItemDto? item)
        {
            if (!ItemValido(item)) return;

            if (!_dialogService.ShowPrompt("Remover conteúdo",
                    $"O conteúdo '{item!.Titulo}' deixará de ser exibido aos estudantes. Informe a justificativa:", "Confirmar remoção", out var justificativa)) return;

            var sucesso = await RunAsync(() => _moderationService.RemoveAsync(item.Tipo, item.ConteudoID, justificativa));
            if (!sucesso) return;

            FilaModeracao.Remove(item);
            SetStatus($"'{item.Titulo}' removido.");
        }

        public void Dispose()
        {
            _ = _moderationService.StopRealtimeAsync();
        }
    }
}
