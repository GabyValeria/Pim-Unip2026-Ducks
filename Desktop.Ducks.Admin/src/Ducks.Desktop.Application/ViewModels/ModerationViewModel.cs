using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;
using Ducks.Desktop.Core.Models;

namespace Ducks.Desktop.Application.ViewModels
{
    public partial class ModerationViewModel : ObservableObject
    {
        private readonly IModerationApiService _moderationService;

        [ObservableProperty]
        private ObservableCollection<ModerationItemDto> _filaModeracao = new();

        [ObservableProperty]
        private ModerationItemDto? _selectedItem;

        [ObservableProperty]
        private bool _isLoading;

        public IAsyncRelayCommand CarregarFilaCommand { get; }
        public IAsyncRelayCommand<ModerationItemDto> AprovarCommand { get; }
        public IAsyncRelayCommand<ModerationItemDto> RejeitarCommand { get; }

        public ModerationViewModel(IModerationApiService moderationService)
        {
            _moderationService = moderationService;
            CarregarFilaCommand = new AsyncRelayCommand(CarregarFilaAsync);
            AprovarCommand = new AsyncRelayCommand<ModerationItemDto>(AprovarAsync);
            RejeitarCommand = new AsyncRelayCommand<ModerationItemDto>(RejeitarAsync);
        }

        public async Task InicializarAsync(Action<Action> dispatchAction)
        {
            await CarregarFilaAsync();
            await _moderationService.InitializeSignalRAsync(newItem =>
            {
                dispatchAction(() =>
                {
                    FilaModeracao.Insert(0, newItem);
                });
            });
        }

        public async Task EncerrarAsync()
        {
            await _moderationService.StopSignalRAsync();
        }

        public async Task CarregarFilaAsync()
        {
            IsLoading = true;
            try
            {
                var itens = await _moderationService.GetPendingQueueAsync();
                FilaModeracao = new ObservableCollection<ModerationItemDto>(itens);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AprovarAsync(ModerationItemDto? item)
        {
            if (item == null) return;
            var sucesso = await _moderationService.ApproveItemAsync(item.ItemID);
            if (sucesso)
            {
                FilaModeracao.Remove(item);
            }
        }

        private async Task RejeitarAsync(ModerationItemDto? item)
        {
            if (item == null) return;
            var sucesso = await _moderationService.RejectItemAsync(item.ItemID);
            if (sucesso)
            {
                FilaModeracao.Remove(item);
            }
        }
    }
}