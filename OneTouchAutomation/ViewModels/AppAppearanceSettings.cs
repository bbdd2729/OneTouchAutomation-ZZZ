using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OneTouchAutomation.ViewModels;

public partial class AppAppearanceSettings : ViewModelBase
{
    [ObservableProperty]
    private Color _acrylicTintColor = Color.Parse("#24364F");

    [ObservableProperty]
    private double _acrylicTintOpacity = 0.74;

    [ObservableProperty]
    private double _acrylicMaterialOpacity = 0.58;

    [ObservableProperty]
    private Color _acrylicFallbackColor = Color.Parse("#202A3A");
}
