using CommunityToolkit.Mvvm.ComponentModel;

namespace Ducks.Admin.Desktop.Stores;

public partial class NavigationStore : ObservableObject
{
    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    public event Action? CurrentViewModelChanged;

    partial void OnCurrentViewModelChanged(ObservableObject? value)
    {
        CurrentViewModelChanged?.Invoke();
    }
}