using System.ComponentModel;
using Avalonia.Media;
using OneTouchAutomation.ViewModels;

namespace OneTouchAutomation.Views;

public partial class MainView : UserControl
{
    private AppAppearanceSettings? _appearance;

    public MainView() { InitializeComponent(); }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_appearance is not null)
        {
            _appearance.PropertyChanged -= OnAppearancePropertyChanged;
        }

        _appearance = (DataContext as MainWindowViewModel)?.Appearance;

        if (_appearance is not null)
        {
            _appearance.PropertyChanged += OnAppearancePropertyChanged;
            ApplyAppearance(_appearance);
        }

        base.OnDataContextChanged(e);
    }

    private void OnAppearancePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is AppAppearanceSettings appearance)
        {
            ApplyAppearance(appearance);
        }
    }

    private void ApplyAppearance(AppAppearanceSettings appearance)
    {
        if (AcrylicHost.Material is not ExperimentalAcrylicMaterial material)
        {
            return;
        }

        material.TintColor       = appearance.AcrylicTintColor;
        material.TintOpacity     = appearance.AcrylicTintOpacity;
        material.MaterialOpacity = appearance.AcrylicMaterialOpacity;
        material.FallbackColor   = appearance.AcrylicFallbackColor;
    }
}