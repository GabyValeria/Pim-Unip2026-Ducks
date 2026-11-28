using System.Windows;
using Ducks.Desktop.Application.ViewModels;

namespace Ducks.Desktop.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Abre a Visão Geral assim que a janela é exibida.
            Loaded += (_, _) => viewModel.NavegarVisaoGeralCommand.Execute(null);
        }
    }
}
