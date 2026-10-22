using CommunityToolkit.Mvvm.ComponentModel;
using Ducks.Admin.Desktop.Stores;

namespace Ducks.Admin.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly NavigationStore _navigationStore;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    public MainViewModel(NavigationStore navigationStore)
    {
        _navigationStore = navigationStore;
        _navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;
        CurrentViewModel = _navigationStore.CurrentViewModel;
    }

    private void OnCurrentViewModelChanged()
    {
        CurrentViewModel = _navigationStore.CurrentViewModel;
    }
}