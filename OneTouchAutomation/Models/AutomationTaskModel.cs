using CommunityToolkit.Mvvm.ComponentModel;

namespace OneTouchAutomation.Models;

public partial class AutomationTaskModel : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [ObservableProperty] private string _behaviorId = "click-template";

    public string BehaviorDisplayName => BehaviorId == "wait-for-template"
        ? "Wait For Template"
        : "Click Template";

    [ObservableProperty] private string _name = "Click template";

    [ObservableProperty] private string _templatePath = string.Empty;

    [ObservableProperty] private double _threshold = 0.85;

    [ObservableProperty] private bool _useRegion;

    [ObservableProperty] private int _regionX;

    [ObservableProperty] private int _regionY;

    [ObservableProperty] private int _regionWidth = 400;

    [ObservableProperty] private int _regionHeight = 300;

    [ObservableProperty] private int _timeoutSeconds = 10;

    [ObservableProperty] private bool _isEnabled = true;

    partial void OnBehaviorIdChanged(string value)
    {
        OnPropertyChanged(nameof(BehaviorDisplayName));
    }
}
