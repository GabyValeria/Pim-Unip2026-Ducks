using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Desktop.Application.Interfaces;

namespace Ducks.Desktop.Application.ViewModels
{
    public class TrilhaItemDto
    {
        public int TrilhaID { get; set; }
        public string NomeTrilha { get; set; } = string.Empty;
        public string Materia { get; set; } = string.Empty;
        public int TotalModulos { get; set; }
        public int TotalAtividades { get; set; }
        public string Status { get; set; } = "APROVADO";
    }

    public partial class CoursesAndTrailsViewModel : ObservableObject
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuthApiService _authService;
        private List<TrilhaItemDto> _todosItens = new();

        [ObservableProperty]
        private ObservableCollection<TrilhaItemDto> _itens = new();

        [ObservableProperty]
        private TrilhaItemDto? _selectedItem;

        [ObservableProperty]
        private int _totalTrilhas;

        [ObservableProperty]
        private int _totalCursos;

        [ObservableProperty]
        private int _totalAtividades;

        [ObservableProperty]
        private string _filtroTermo = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _opcoesStatus = new() { "Todos", "APROVADO", "PENDENTE", "REJEITADO" };

        [ObservableProperty]
        private string _filtroStatusSelecionado = "Todos";

        [ObservableProperty]
        private string _mensagemStatus = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        public IAsyncRelayCommand CarregarDadosCommand { get; }

        public CoursesAndTrailsViewModel(IHttpClientFactory httpClientFactory, IAuthApiService authService)
        {
            _httpClientFactory = httpClientFactory;
            _authService = authService;
            CarregarDadosCommand = new AsyncRelayCommand(CarregarDadosAsync);

            _ = CarregarDadosAsync();
        }

        partial void OnFiltroTermoChanged(string value) => AplicarFiltros();
        partial void OnFiltroStatusSelecionadoChanged(string value) => AplicarFiltros();

        public async Task CarregarDadosAsync()
        {
            IsLoading = true;
            MensagemStatus = "Carregando catálogo educacional...";
            try
            {
                var client = _httpClientFactory.CreateClient("DucksApi");
                if (!string.IsNullOrEmpty(_authService.CurrentToken))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authService.CurrentToken);
                }

                var response = await client.GetFromJsonAsync<List<TrilhaItemDto>>("api/admin/content/trilhas");
                
                if (response != null && response.Any())
                {
                    _todosItens = response;
                }
                else
                {
                    _todosItens = new List<TrilhaItemDto>
                    {
                        new TrilhaItemDto { TrilhaID = 1, NomeTrilha = "Trilha Completa de C# e .NET 10", Materia = "Tecnologia e Desenvolvimento", TotalModulos = 4, TotalAtividades = 16, Status = "APROVADO" },
                        new TrilhaItemDto { TrilhaID = 2, NomeTrilha = "Arquitetura Limpa e Microsserviços", Materia = "Engenharia de Software", TotalModulos = 3, TotalAtividades = 12, Status = "APROVADO" },
                        new TrilhaItemDto { TrilhaID = 3, NomeTrilha = "Desenvolvimento Web com Blazor e WPF", Materia = "Frontend & Desktop", TotalModulos = 5, TotalAtividades = 20, Status = "PENDENTE" }
                    };
                }

                AtualizarMetricas();
                AplicarFiltros();
                MensagemStatus = "Catálogo carregado com sucesso.";
            }
            catch (Exception)
            {
                _todosItens = new List<TrilhaItemDto>
                {
                    new TrilhaItemDto { TrilhaID = 1, NomeTrilha = "Trilha Completa de C# e .NET 10", Materia = "Tecnologia e Desenvolvimento", TotalModulos = 4, TotalAtividades = 16, Status = "APROVADO" },
                    new TrilhaItemDto { TrilhaID = 2, NomeTrilha = "Arquitetura Limpa e Microsserviços", Materia = "Engenharia de Software", TotalModulos = 3, TotalAtividades = 12, Status = "APROVADO" },
                    new TrilhaItemDto { TrilhaID = 3, NomeTrilha = "Desenvolvimento Web com Blazor e WPF", Materia = "Frontend & Desktop", TotalModulos = 5, TotalAtividades = 20, Status = "PENDENTE" }
                };
                AtualizarMetricas();
                AplicarFiltros();
                MensagemStatus = "Catálogo carregado (Modo Demonstração).";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AtualizarMetricas()
        {
            TotalTrilhas = _todosItens.Count;
            TotalCursos = _todosItens.Sum(x => x.TotalModulos);
            TotalAtividades = _todosItens.Sum(x => x.TotalAtividades);
        }

        private void AplicarFiltros()
        {
            var resultado = _todosItens.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(FiltroTermo))
            {
                resultado = resultado.Where(x => 
                    x.NomeTrilha.Contains(FiltroTermo, StringComparison.OrdinalIgnoreCase) || 
                    x.Materia.Contains(FiltroTermo, StringComparison.OrdinalIgnoreCase));
            }

            if (FiltroStatusSelecionado != "Todos")
            {
                resultado = resultado.Where(x => string.Equals(x.Status, FiltroStatusSelecionado, StringComparison.OrdinalIgnoreCase));
            }

            Itens = new ObservableCollection<TrilhaItemDto>(resultado);
        }
    }
}