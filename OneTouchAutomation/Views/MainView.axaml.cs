using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using AvaloniaFluentUI.Controls;
using AvaloniaFluentUI.Styling;
using OneTouchAutomation.Models;
using OneTouchAutomation.ViewModels;

namespace OneTouchAutomation.Views;

public partial class MainView : UserControl
{
    private AppAppearanceSettings? _appearance;
    private MainWindowViewModel? _mainWindowViewModel;

    public MainView()
    {
        InitializeComponent();
        AvaloniaFluentTheme.Instance.ThemeChanged += OnThemeChanged;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_appearance is not null)
        {
            _appearance.PropertyChanged -= OnAppearancePropertyChanged;
        }

        if (_mainWindowViewModel is not null)
        {
            _mainWindowViewModel.PropertyChanged -= OnMainWindowViewModelPropertyChanged;
        }

        _mainWindowViewModel = DataContext as MainWindowViewModel;
        _appearance = _mainWindowViewModel?.Appearance;

        if (_appearance is not null)
        {
            _appearance.PropertyChanged += OnAppearancePropertyChanged;
            ApplyWindowMaterialBackground(_appearance);
        }

        if (_mainWindowViewModel is not null)
        {
            _mainWindowViewModel.PropertyChanged += OnMainWindowViewModelPropertyChanged;
            SyncNavigationSelection(_mainWindowViewModel.SelectedListItem);
        }

        base.OnDataContextChanged(e);
    }

    private void OnAppearancePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppAppearanceSettings.WindowMaterialMode) && sender is AppAppearanceSettings appearance)
        {
            ApplyWindowMaterialBackground(appearance);
        }
    }

    private void ApplyWindowMaterialBackground(AppAppearanceSettings appearance)
    {
        if (appearance.WindowMaterialMode == WindowMaterialMode.None)
        {
            if (Application.Current?.TryGetResource("SystemBrush", ActualThemeVariant, out var resource) == true
                && resource is IBrush systemBrush)
            {
                Background = systemBrush;
            }
            return;
        }

        Background = Brushes.Transparent;
    }

    private void OnThemeChanged(object? sender, Avalonia.Styling.ThemeVariant? e)
    {
        if (_appearance is not null)
        {
            ApplyWindowMaterialBackground(_appearance);
        }
    }

    private void NavigationView_SelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if(e.SelectedItem is NavigationViewItem { Tag: SideBarItemModel item }
           && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SelectedListItem = item;
        }
    }

    private void OnMainWindowViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.SelectedListItem) && sender is MainWindowViewModel viewModel)
        {
            SyncNavigationSelection(viewModel.SelectedListItem);
        }
    }

    private void SyncNavigationSelection(SideBarItemModel? selectedItem)
    {
        if (selectedItem is null)
        {
            return;
        }

        var navigationItem = NavigationView.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => Equals(item.Tag, selectedItem));

        if (navigationItem is not null && !ReferenceEquals(NavigationView.SelectedItem, navigationItem))
        {
            NavigationView.SelectedItem = navigationItem;
        }
    }
}
