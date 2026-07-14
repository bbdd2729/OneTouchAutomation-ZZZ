using CommunityToolkit.Mvvm.ComponentModel;

namespace OneTouchAutomation.ViewModels;

public partial class AppAppearanceSettings : ViewModelBase
{
    [ObservableProperty]
    private WindowMaterialMode _windowMaterialMode = WindowMaterialMode.Mica;
}

public enum WindowMaterialMode
{
    None,
    Mica,
    Acrylic,
}
