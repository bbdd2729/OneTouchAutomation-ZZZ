using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OneTouchAutomation.ViewModels;

public partial class SettingsPageViewModel : ViewModelBase
{
    public SettingsPageViewModel(AppAppearanceSettings appearance)
    {
        Appearance = appearance;
        ThemeColors = new ObservableCollection<AcrylicThemeColorOption>
        {
            new("Ocean", Color.Parse("#24364F"), Color.Parse("#202A3A")),
            new("Graphite", Color.Parse("#2D3038"), Color.Parse("#24272E")),
            new("Teal", Color.Parse("#1F4944"), Color.Parse("#1B3936")),
            new("Violet", Color.Parse("#44365F"), Color.Parse("#342A49")),
            new("Obsidian", Color.Parse("#4B342C"), Color.Parse("#362923")),
        };

        SelectedThemeColor = ThemeColors[0];
    }

    public SettingsPageViewModel() : this(new AppAppearanceSettings()) { }

    public AppAppearanceSettings Appearance { get; }

    public ObservableCollection<AcrylicThemeColorOption> ThemeColors { get; }

    [ObservableProperty]
    private AcrylicThemeColorOption? _selectedThemeColor;

    partial void OnSelectedThemeColorChanged(AcrylicThemeColorOption? value)
    {
        if (value is null) return;

        Appearance.AcrylicTintColor = value.TintColor;
        Appearance.AcrylicFallbackColor = value.FallbackColor;
    }

    [RelayCommand]
    private void SelectThemeColor(AcrylicThemeColorOption? option)
    {
        if (option is null) return;

        SelectedThemeColor = option;
    }
}

public sealed record AcrylicThemeColorOption(string Name, Color TintColor, Color FallbackColor);
