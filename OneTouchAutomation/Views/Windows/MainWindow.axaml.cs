using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using AvaloniaFluentUI.Controls;
using AvaloniaFluentUI.Icons;
using AvaloniaFluentUI.Styling;
using AvaloniaFluentUI.Windowing;
using OneTouchAutomation.ViewModels;

namespace OneTouchAutomation.Views;

public partial class MainWindow : FluentWindow
{
    private readonly AppAppearanceSettings _appearance;
    private bool _isOpened;

    public MainWindow(MainWindowViewModel vm)
    {
        _appearance = vm.Appearance;
        DataContext = vm;
        InitializeComponent();
        _appearance.PropertyChanged += OnAppearancePropertyChanged;
        AvaloniaFluentTheme.Instance.ThemeChanged += OnThemeChanged;
        Opened += OnOpened;
    }

    public MainWindow() : this(new MainWindowViewModel()) { }

    private void OnToggleTopmost(object? sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;

        PinButton.Content = Topmost ? FluentIcon.Unpin : FluentIcon.Pin;
        ToolTip.SetTip(PinButton, Topmost ? "Disable always on top" : "Always on top");
    }

    private void OnToggleTheme(object? sender, RoutedEventArgs e)
    {
        AvaloniaFluentTheme.Instance.CurrentTheme = AvaloniaFluentTheme.Instance.CurrentTheme == ThemeVariant.Light
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }

    private void OnAppearancePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppAppearanceSettings.WindowMaterialMode))
        {
            ApplyWindowMaterial();
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        _isOpened = true;
        ApplyWindowMaterial();
    }

    private void OnThemeChanged(object? sender, ThemeVariant? e)
    {
        if (_isOpened)
        {
            ApplyWindowMaterial();
        }
    }

    private void ApplyWindowMaterial()
    {
        switch (_appearance.WindowMaterialMode)
        {
            case WindowMaterialMode.Mica:
                EnabledAcrylicBlue(false);
                EnabledMica(true);
                break;
            case WindowMaterialMode.Acrylic:
                EnabledMica(false);
                EnabledAcrylicBlue(true);
                break;
            default:
                EnabledAcrylicBlue(false);
                EnabledMica(false);
                break;
        }
    }
}
