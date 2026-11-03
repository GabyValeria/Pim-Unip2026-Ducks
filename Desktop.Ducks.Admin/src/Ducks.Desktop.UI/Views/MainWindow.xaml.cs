using System.Windows;
using Ducks.Desktop.UI.ViewModels;

namespace Ducks.Desktop.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel; 
        }
    }
}