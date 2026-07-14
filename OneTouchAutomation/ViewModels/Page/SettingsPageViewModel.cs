using Avalonia.Styling;
using Avalonia.Media;
using AvaloniaFluentUI.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OneTouchAutomation.ViewModels;

public partial class SettingsPageViewModel : ViewModelBase
{
    private bool _isUpdatingAccentSelection;

    public SettingsPageViewModel(AppAppearanceSettings appearance)
    {
        Appearance = appearance;
        Appearance.PropertyChanged += OnAppearancePropertyChanged;
        AvaloniaFluentTheme.Instance.ThemeChanged += OnThemeChanged;
    }

    public SettingsPageViewModel() : this(new AppAppearanceSettings()) { }

    public AppAppearanceSettings Appearance { get; }

    public bool IsNoWindowMaterial => Appearance.WindowMaterialMode == WindowMaterialMode.None;

    public bool IsMicaWindowMaterial => Appearance.WindowMaterialMode == WindowMaterialMode.Mica;

    public bool IsAcrylicWindowMaterial => Appearance.WindowMaterialMode == WindowMaterialMode.Acrylic;

    public bool IsWindows11 => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

    public bool IsSystemTheme => AvaloniaFluentTheme.Instance.CurrentTheme == ThemeVariant.Default;

    public bool IsLightTheme => AvaloniaFluentTheme.Instance.CurrentTheme == ThemeVariant.Light;

    public bool IsDarkTheme => AvaloniaFluentTheme.Instance.CurrentTheme == ThemeVariant.Dark;

    [ObservableProperty]
    private bool _isDefaultAccentColor = true;

    [ObservableProperty]
    private bool _isCustomAccentColor;

    [ObservableProperty]
    private Color _selectedAccentColor = Color.Parse("#0078D4");

    private void OnAppearancePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppAppearanceSettings.WindowMaterialMode))
        {
            OnPropertyChanged(nameof(IsNoWindowMaterial));
            OnPropertyChanged(nameof(IsMicaWindowMaterial));
            OnPropertyChanged(nameof(IsAcrylicWindowMaterial));
        }
    }

    private void OnThemeChanged(object? sender, ThemeVariant? e)
    {
        OnPropertyChanged(nameof(IsSystemTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsDarkTheme));
    }

    partial void OnIsDefaultAccentColorChanged(bool value)
    {
        if (!value || _isUpdatingAccentSelection)
        {
            return;
        }

        _isUpdatingAccentSelection = true;
        IsCustomAccentColor = false;
        AvaloniaFluentTheme.Instance.CustomAccentColor = null;
        _isUpdatingAccentSelection = false;
    }

    partial void OnIsCustomAccentColorChanged(bool value)
    {
        if (!value || _isUpdatingAccentSelection)
        {
            return;
        }

        _isUpdatingAccentSelection = true;
        IsDefaultAccentColor = false;
        AvaloniaFluentTheme.Instance.CustomAccentColor = SelectedAccentColor;
        _isUpdatingAccentSelection = false;
    }

    partial void OnSelectedAccentColorChanged(Color value)
    {
        if (IsCustomAccentColor)
        {
            AvaloniaFluentTheme.Instance.CustomAccentColor = value;
        }
    }

    [RelayCommand]
    private void SelectWindowMaterial(WindowMaterialMode mode)
    {
        Appearance.WindowMaterialMode = mode;
    }

    [RelayCommand]
    private void SelectTheme(AppThemeMode mode)
    {
        AvaloniaFluentTheme.Instance.CurrentTheme = mode switch
        {
            AppThemeMode.Light => ThemeVariant.Light,
            AppThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}

public enum AppThemeMode
{
    System,
    Light,
    Dark,
}
