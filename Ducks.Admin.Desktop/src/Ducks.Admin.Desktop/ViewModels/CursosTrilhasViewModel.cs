using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ducks.Admin.Desktop.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Ducks.Admin.Desktop.ViewModels
{
    public partial class CursosTrilhasViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<CursoTrilhaDto> _cursosTrilhas = new();

        [ObservableProperty]
        private string _statusMensagem = "Gerenciamento de Cursos e Trilhas carregado.";

        public CursosTrilhasViewModel()
        {
            CarregarDadosIniciais();
        }

        [RelayCommand]
        private void CarregarDadosIniciais()
        {
            CursosTrilhas = new ObservableCollection<CursoTrilhaDto>
            {
                new CursoTrilhaDto { CursoID = 1, Titulo = "Desenvolvimento C# Avançado", Categoria = "Programação", CargaHoraria = 40, Publicado = true },
                new CursoTrilhaDto { CursoID = 2, Titulo = "Fundamentos de Arquitetura Limpa (Clean Code)", Categoria = "Engenharia de Software", CargaHoraria = 30, Publicado = true },
                new CursoTrilhaDto { CursoID = 3, Titulo = "Gamificação em Plataformas Educacionais", Categoria = "Design Educacional", CargaHoraria = 20, Publicado = false }
            };
            StatusMensagem = "Lista de cursos e trilhas atualizada com sucesso.";
        }

        [RelayCommand]
        private async Task AdicionarNovoCursoAsync()
        {
            CursosTrilhas.Add(new CursoTrilhaDto
            {
                CursoID = CursosTrilhas.Count + 1,
                Titulo = "Novo Curso Ducks",
                Categoria = "Geral",
                CargaHoraria = 15,
                Publicado = false
            });
            StatusMensagem = "Novo curso adicionado para configuração.";
            await Task.CompletedTask;
        }
    }
}