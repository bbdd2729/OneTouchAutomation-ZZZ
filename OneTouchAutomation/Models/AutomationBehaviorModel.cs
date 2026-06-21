using CommunityToolkit.Mvvm.ComponentModel;

namespace OneTouchAutomation.Models;

public partial class AutomationBehaviorModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "New behavior";

    [ObservableProperty]
    private string _target = "Game window";

    [ObservableProperty]
    private string _trigger = "Image match";

    [ObservableProperty]
    private string _action = "Click";

    [ObservableProperty]
    private double _confidence = 0.85;

    [ObservableProperty]
    private bool _isEnabled = true;
}
