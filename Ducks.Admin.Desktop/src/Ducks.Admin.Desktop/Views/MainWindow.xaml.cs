using Ducks.Admin.Desktop.ViewModels;
using System.Windows;

namespace Ducks.Admin.Desktop.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }
    }
}